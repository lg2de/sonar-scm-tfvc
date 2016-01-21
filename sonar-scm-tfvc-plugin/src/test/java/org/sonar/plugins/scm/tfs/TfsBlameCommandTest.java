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
  public void Setup() {
    appender = new TestAppender();

    getRootLogger().addAppender(appender);
  }

  @After
  public void tearDown() {
    getRootLogger().detachAppender(appender);
  }

  @Test
  public void ok() throws IOException {
    File executable = new File("src/test/resources/fake.bat");
    TfsBlameCommand command = new TfsBlameCommand(conf, executable);

    File file = new File("src/test/resources/ok.txt");
    DefaultInputFile inputFile = new DefaultInputFile("ok", "ok.txt").setAbsolutePath(file.getAbsolutePath());

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
  public void should_annotate_last_empty_line() {
    File executable = new File("src/test/resources/fake.bat");
    TfsBlameCommand command = new TfsBlameCommand(conf, executable);

    File file = new File("src/test/resources/ok.txt");
    DefaultInputFile inputFile = new DefaultInputFile("ok", "ok.txt").setAbsolutePath(file.getAbsolutePath());
    inputFile.setLines(3);

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
  public void should_fail_on_invalid_output() {
    File executable = new File("src/test/resources/fake.bat");
    TfsBlameCommand command = new TfsBlameCommand(conf, executable);

    File file = new File("src/test/resources/invalid_output.txt");
    DefaultInputFile inputFile = new DefaultInputFile("invalid_output", "invalid_output.txt").setAbsolutePath(file.getAbsolutePath());

    BlameInput input = mock(BlameInput.class);
    when(input.filesToBlame()).thenReturn(Arrays.<InputFile>asList(inputFile));

    BlameOutput output = mock(BlameOutput.class);

    command.blame(input, output);
    assertThat(appender.getErrorEvents()).containsExactly("" +
            "IllegalStateException thrown in the TFVC annotate command : Invalid output from the TFVC annotate command: \"hello world!\" on file: " + inputFile.absolutePath() + " at line 1");
    verify(output, Mockito.never()).blameResult(Mockito.any(InputFile.class), Mockito.anyList());
  }

  @Test
  public void should_fail_on_error_on_file() {
    File executable = new File("src/test/resources/file_level_error.bat");
    TfsBlameCommand command = new TfsBlameCommand(conf, executable);

    File file = new File("src/test/resources/ko_non_existing.txt");
    DefaultInputFile inputFile = new DefaultInputFile("ko_non_existing", "ko_non_existing.txt").setAbsolutePath(file.getAbsolutePath());

    File file2 = new File("src/test/resources/ok.txt");
    DefaultInputFile inputFile2 = new DefaultInputFile("ok", "ok.txt").setAbsolutePath(file2.getAbsolutePath());


    BlameInput input = mock(BlameInput.class);
    when(input.filesToBlame()).thenReturn(Arrays.<InputFile>asList(inputFile,inputFile2));
    BlameOutput output = mock(BlameOutput.class);

    command.blame(input, output);
    assertThat(appender.getErrorEvents()).containsExactly("" +
            "Exception on Annotating File");
    verify(output).blameResult(
            inputFile2,
            Arrays.asList(
                    new BlameLine().date(new Date(1430736199000L)).revision("26274").author("SND\\DinSoft_cp"),
                    new BlameLine().date(new Date(1430736200000L)).revision("26275").author("SND\\DinSoft_cp")));
  }

  @Test
  public void should_fail_on_error_on_project() {
    File executable = new File("src/test/resources/project_level_error.bat");
    TfsBlameCommand command = new TfsBlameCommand(conf, executable);

    File file = new File("src/test/resources/ko_non_existing.txt");
    DefaultInputFile inputFile = new DefaultInputFile("ko_non_existing", "ko_non_existing.txt").setAbsolutePath(file.getAbsolutePath());


    BlameInput input = mock(BlameInput.class);
    when(input.filesToBlame()).thenReturn(Arrays.<InputFile>asList(inputFile));
    BlameOutput output = mock(BlameOutput.class);

    command.blame(input, output);
    assertThat(appender.getErrorEvents()).containsExactly("" +
            "Exception on Annotating Project");
    verify(output, Mockito.never()).blameResult(Mockito.any(InputFile.class), Mockito.anyList());
  }

  @Test
  public void read_error_stream() {
    File executable = new File("src/test/resources/error_stream.bat");
    TfsBlameCommand command = new TfsBlameCommand(conf, executable);
    command.blame(mock(BlameInput.class), mock(BlameOutput.class));
    String[] errArray = {"IOException thrown in the TFVC annotate command : The pipe is being closed","error stream string 1\r\nerror stream string 2\r\n"};
    assertThat(appender.getErrorEvents()).containsExactly(errArray);
  }

  private static Logger getRootLogger() {
    Logger rootLogger = (Logger) LoggerFactory.getLogger(Logger.ROOT_LOGGER_NAME);
    return rootLogger.getLoggerContext().getLogger(Logger.ROOT_LOGGER_NAME);
  }
}
