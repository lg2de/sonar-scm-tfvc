/*
 * SonarQube :: SCM :: TFVC :: Plugin
 * Copyright (C) 2014 SonarSource
 * sonarqube@googlegroups.com
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public
 * License along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02
 */
package org.sonar.plugins.scm.tfs;

import org.junit.Rule;
import org.junit.Test;
import org.junit.rules.ExpectedException;
import org.mockito.Mockito;
import org.sonar.api.batch.fs.InputFile;
import org.sonar.api.batch.fs.internal.DefaultInputFile;
import org.sonar.api.batch.scm.BlameCommand.BlameInput;
import org.sonar.api.batch.scm.BlameCommand.BlameOutput;
import org.sonar.api.batch.scm.BlameLine;

import java.io.File;
import java.io.IOException;
import java.util.Arrays;
import java.util.Date;

import static org.mockito.Mockito.mock;
import static org.mockito.Mockito.verify;
import static org.mockito.Mockito.when;

public class TfsBlameCommandTest {

  @Rule
  public ExpectedException thrown = ExpectedException.none();

  private final TfsConfiguration conf = mock(TfsConfiguration.class);

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
  }

  @Test
  public void should_not_save_info_when_0_lines_returned() throws IOException {
    File executable = new File("src/test/resources/fake.bat");
    TfsBlameCommand command = new TfsBlameCommand(conf, executable);

    File file = new File("src/test/resources/ko_0_lines.txt");
    DefaultInputFile inputFile = new DefaultInputFile("ko_0_lines", "ko_0_lines.txt").setAbsolutePath(file.getAbsolutePath());

    BlameInput input = mock(BlameInput.class);
    when(input.filesToBlame()).thenReturn(Arrays.<InputFile>asList(inputFile));

    BlameOutput output = mock(BlameOutput.class);

    command.blame(input, output);

    verify(output, Mockito.never()).blameResult(Mockito.any(InputFile.class), Mockito.anyList());
  }

  @Test
  public void should_fail_on_invalid_output() {
    File executable = new File("src/test/resources/fake.bat");
    TfsBlameCommand command = new TfsBlameCommand(conf, executable);

    File file = new File("src/test/resources/invalid_output.txt");
    DefaultInputFile inputFile = new DefaultInputFile("invalid_output", "invalid_output.txt").setAbsolutePath(file.getAbsolutePath());

    thrown.expect(IllegalStateException.class);
    thrown.expectMessage("Invalid output from the TFVC annotate command: \"hello world!\" on file:");
    thrown.expectMessage("at line 1");

    BlameInput input = mock(BlameInput.class);
    when(input.filesToBlame()).thenReturn(Arrays.<InputFile>asList(inputFile));

    command.blame(input, mock(BlameOutput.class));
  }

  @Test
  public void file_not_found_error() {
    File executable = new File("src/test/resources/fake.bat");
    TfsBlameCommand command = new TfsBlameCommand(conf, executable);

    File file = new File("src/test/resources/ko_non_existing.txt");
    DefaultInputFile inputFile = new DefaultInputFile("ko_non_existing", "ko_non_existing.txt").setAbsolutePath(file.getAbsolutePath());

    thrown.expect(IllegalStateException.class);
    thrown.expectMessage("The TFVC annotate command " + executable.getAbsolutePath() + " failed with exit code 1");

    BlameInput input = mock(BlameInput.class);
    when(input.filesToBlame()).thenReturn(Arrays.<InputFile>asList(inputFile));

    command.blame(input, mock(BlameOutput.class));
  }

}
