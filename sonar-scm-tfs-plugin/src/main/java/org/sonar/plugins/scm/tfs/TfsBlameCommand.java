/*
 * SonarQube :: SCM :: TFS :: Plugin
 * Copyright (C) 2014 SonarSource
 * dev@sonar.codehaus.org
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

import com.google.common.annotations.VisibleForTesting;
import com.google.common.base.Charsets;
import com.google.common.base.Throwables;
import com.google.common.io.Closeables;
import com.google.common.io.Files;
import com.google.common.io.Resources;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.sonar.api.batch.fs.InputFile;
import org.sonar.api.batch.scm.BlameCommand;
import org.sonar.api.batch.scm.BlameLine;
import org.sonar.api.utils.TempFolder;

import java.io.BufferedReader;
import java.io.File;
import java.io.IOException;
import java.io.InputStreamReader;
import java.io.OutputStreamWriter;
import java.util.List;

public class TfsBlameCommand extends BlameCommand {

  private static final Logger LOG = LoggerFactory.getLogger(TfsBlameCommand.class);
  private final File executable;

  public TfsBlameCommand(TempFolder temp) {
    this(extractExecutable(temp));
  }

  @VisibleForTesting
  public TfsBlameCommand(File executable) {
    this.executable = executable;
  }

  @Override
  public void blame(BlameInput input, BlameOutput output) {
    for (InputFile inputFile : input.filesToBlame()) {
      Process process = null;
      try {
        LOG.debug("Executing the TFS blame command: " + executable.getAbsolutePath() + " " + inputFile.absolutePath());
        process = new ProcessBuilder(executable.getAbsolutePath(), inputFile.absolutePath()).start();

        OutputStreamWriter stdin = new OutputStreamWriter(process.getOutputStream(), Charsets.UTF_8);
        stdin.close();

        BufferedReader stdout = new BufferedReader(new InputStreamReader(process.getInputStream(), Charsets.UTF_8));
        TfsBlameConsumer consumer = new TfsBlameConsumer(inputFile.relativePath());
        consumer.process(stdout);

        int exitCode = process.waitFor();
        if (exitCode != 0) {
          throw new IllegalStateException("The TFS blame command " + executable.getAbsolutePath() + " " + inputFile.absolutePath() + " failed with exit code " + exitCode);
        }

        List<BlameLine> lines = consumer.getLines();
        if (lines.size() == inputFile.lines() - 1) {
          // SONARPLUGINS-3097 TFS do not report blame on last empty line
          lines.add(lines.get(lines.size() - 1));
        }

        output.blameResult(inputFile, consumer.getLines());
      } catch (IOException e) {
        throw Throwables.propagate(e);
      } catch (InterruptedException e) {
        throw Throwables.propagate(e);
      } finally {
        if (process != null) {
          Closeables.closeQuietly(process.getInputStream());
          Closeables.closeQuietly(process.getOutputStream());
          Closeables.closeQuietly(process.getErrorStream());
        }
      }
    }
  }

  private static File extractExecutable(TempFolder temp) {
    File executable = temp.newFile("SonarTfsAnnotate", ".exe");
    try {
      Files.write(Resources.toByteArray(TfsBlameCommand.class.getResource("/SonarTfsAnnotate.exe")), executable);
    } catch (IOException e) {
      throw new IllegalStateException("Unable to extract SonarTfsAnnotate.exe", e);
    }
    return executable;
  }

}
