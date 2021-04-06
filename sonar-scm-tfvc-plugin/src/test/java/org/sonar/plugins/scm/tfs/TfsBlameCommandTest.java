/*
 * SonarQube :: SCM :: TFVC :: Plugin
 * Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
 *
 * Licensed under the MIT License. See License.txt in the project root for license information.
 */
package org.sonar.plugins.scm.tfs;

import ch.qos.logback.classic.Logger;
import java.io.File;
import java.io.IOException;
import java.util.Arrays;
import java.util.Date;
import org.junit.After;
import org.junit.Before;
import org.junit.Rule;
import org.junit.Test;
import org.junit.rules.ExpectedException;
import org.mockito.Mockito;
import org.slf4j.LoggerFactory;
import org.sonar.api.batch.fs.InputFile;
import org.sonar.api.batch.fs.internal.DefaultInputFile;
import org.sonar.api.batch.fs.internal.TestInputFileBuilder;
import org.sonar.api.batch.scm.BlameCommand.BlameInput;
import org.sonar.api.batch.scm.BlameCommand.BlameOutput;
import org.sonar.api.batch.scm.BlameLine;
import org.sonar.plugins.scm.tfs.helpers.TestAppender;

import static org.fest.assertions.Assertions.assertThat;
import static org.mockito.Mockito.mock;
import static org.mockito.Mockito.verify;
import static org.mockito.Mockito.when;

public class TfsBlameCommandTest {

  @Rule
  public ExpectedException thrown = ExpectedException.none();

  private final TfsConfiguration conf = mock(TfsConfiguration.class);

  private TestAppender appender;

  @Before
  public void setup() {
    appender = new TestAppender();
    getRootLogger().addAppender(appender);

    when(conf.collectionUri()).thenReturn("https://localtfs/tfs");
  }

  @After
  public void tearDown() {
    getRootLogger().detachAppender(appender);
  }

  @Test
  public void init_missingCollectionUri_noExceptionThrown() {
    File executable = new File("src/test/resources/fake.bat");
    when(conf.collectionUri()).thenReturn("");

    TfsBlameCommand command = new TfsBlameCommand(conf, executable);
    assertThat(command).isNotNull();
    assertThat(appender.getErrorEvents()).isEmpty();
    assertThat(appender.getWarningEvents()).containsExactly(
        "SCM-TFVC: Missing configuration for CollectionUri. The project may not receive blame information.");
  }

  @Test
  public void blame_sampleData_processedWithoutError() throws IOException {
    File executable = new File("src/test/resources/fake.bat");
    TfsBlameCommand command = new TfsBlameCommand(conf, executable);
    File file = new File("src/test/resources/ok.txt");
    DefaultInputFile inputFile = new TestInputFileBuilder("ok", "ok.txt")
        .setModuleBaseDir(file.toPath().getParent())
        .build();
    BlameInput input = mock(BlameInput.class);
    when(input.filesToBlame()).thenReturn(Arrays.<InputFile>asList(inputFile));
    BlameOutput output = mock(BlameOutput.class);

    command.blame(input, output);

    verify(output).blameResult(
        inputFile,
        Arrays.asList(
            new BlameLine().date(new Date(1430736199000L)).revision("26274").author("SND\\DinSoft_cp"),
            new BlameLine().date(new Date(1430736200000L)).revision("26275").author("SND\\DinSoft_cp")));
    assertThat(appender.getErrorEvents()).isEmpty();
  }

  @Test
  public void blame_lastLineEmpty_annotated() {
    File executable = new File("src/test/resources/fake.bat");
    TfsBlameCommand command = new TfsBlameCommand(conf, executable);
    File file = new File("src/test/resources/ok.txt");
    DefaultInputFile inputFile = new TestInputFileBuilder("ok", "ok.txt")
        .setModuleBaseDir(file.toPath().getParent())
        .setLines(3)
        .build();
    BlameInput input = mock(BlameInput.class);
    when(input.filesToBlame()).thenReturn(Arrays.<InputFile>asList(inputFile));
    BlameOutput output = mock(BlameOutput.class);

    command.blame(input, output);

    verify(output).blameResult(
        inputFile,
        Arrays.asList(
            new BlameLine().date(new Date(1430736199000L)).revision("26274").author("SND\\DinSoft_cp"),
            new BlameLine().date(new Date(1430736200000L)).revision("26275").author("SND\\DinSoft_cp"),
            new BlameLine().date(new Date(1430736200000L)).revision("26275").author("SND\\DinSoft_cp")));

    assertThat(appender.getErrorEvents()).isEmpty();
  }

  @Test
  public void blame_invalidOutput_errorLogged() {
    File executable = new File("src/test/resources/fake.bat");
    TfsBlameCommand command = new TfsBlameCommand(conf, executable);
    File file = new File("src/test/resources/invalid_output.txt");
    DefaultInputFile inputFile = new TestInputFileBuilder("invalid_output", "invalid_output.txt")
        .setModuleBaseDir(file.toPath().getParent())
        .build();
    BlameInput input = mock(BlameInput.class);
    when(input.filesToBlame()).thenReturn(Arrays.<InputFile>asList(inputFile));
    BlameOutput output = mock(BlameOutput.class);

    command.blame(input, output);

    assertThat(appender.getErrorEvents()).containsExactly(
        "SCM-TFVC: IllegalStateException thrown in the TFVC annotate command: Invalid output from the TFVC annotate command: \"hello world!\" on file: " + inputFile.absolutePath() + " at line 1");
    verify(output, Mockito.never()).blameResult(Mockito.any(InputFile.class), Mockito.anyListOf(BlameLine.class));
  }

  @Test
  public void blame_exceptionWhileFileProcessing_errorLogged() {
    File executable = new File("src/test/resources/file_level_error.bat");
    TfsBlameCommand command = new TfsBlameCommand(conf, executable);
    File file = new File("src/test/resources/ko_non_existing.txt");
    DefaultInputFile inputFile = new TestInputFileBuilder("ko_non_existing", "ko_non_existing.txt")
        .setModuleBaseDir(file.toPath().getParent())
        .build();
    File file2 = new File("src/test/resources/ok.txt");
    DefaultInputFile inputFile2 = new TestInputFileBuilder("ok", "ok.txt")
        .setModuleBaseDir(file2.toPath().getParent())
        .build();
    BlameInput input = mock(BlameInput.class);
    when(input.filesToBlame()).thenReturn(Arrays.<InputFile>asList(inputFile,inputFile2));
    BlameOutput output = mock(BlameOutput.class);

    command.blame(input, output);

    assertThat(appender.getErrorEvents()).containsExactly("SCM-TFVC: Exception on Annotating File");
    verify(output).blameResult(
        inputFile2,
        Arrays.asList(
            new BlameLine().date(new Date(1430736199000L)).revision("26274").author("SND\\DinSoft_cp"),
            new BlameLine().date(new Date(1430736200000L)).revision("26275").author("SND\\DinSoft_cp")));
  }

  @Test
  public void blame_exceptionWhileProjectProcessing_errorLogged() {
    File executable = new File("src/test/resources/project_level_error.bat");
    TfsBlameCommand command = new TfsBlameCommand(conf, executable);
    File file = new File("src/test/resources/ko_non_existing.txt");
    DefaultInputFile inputFile = new TestInputFileBuilder("ko_non_existing", "ko_non_existing.txt")
        .setModuleBaseDir(file.toPath().getParent())
        .build();
    BlameInput input = mock(BlameInput.class);
    when(input.filesToBlame()).thenReturn(Arrays.<InputFile>asList(inputFile));
    BlameOutput output = mock(BlameOutput.class);

    command.blame(input, output);

    assertThat(appender.getErrorEvents()).containsExactly("SCM-TFVC: Exception on Annotating Project");
    verify(output, Mockito.never()).blameResult(Mockito.any(InputFile.class), Mockito.anyListOf(BlameLine.class));
  }

  @Test
  public void blame_exceptionInStream_errorLogged() {
    File executable = new File("src/test/resources/error_stream.bat");
    TfsBlameCommand command = new TfsBlameCommand(conf, executable);

    command.blame(mock(BlameInput.class), mock(BlameOutput.class));

    assertThat(appender.getErrorEvents().get(0)).startsWith("SCM-TFVC: IOException thrown in the TFVC annotate command:");
    assertThat(appender.getErrorEvents().get(1)).isEqualTo("SCM-TFVC: error stream string 1 \r\nerror stream string 2 \r\n");
  }

  private static Logger getRootLogger() {
    Logger rootLogger = (Logger) LoggerFactory.getLogger(Logger.ROOT_LOGGER_NAME);
    return rootLogger.getLoggerContext().getLogger(Logger.ROOT_LOGGER_NAME);
  }
}
