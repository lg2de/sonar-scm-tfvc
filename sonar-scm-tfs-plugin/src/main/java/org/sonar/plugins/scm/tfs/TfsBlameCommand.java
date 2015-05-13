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
import com.google.common.collect.Lists;
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
import java.util.Date;
import java.util.List;
import java.util.regex.Matcher;
import java.util.regex.Pattern;

public class TfsBlameCommand extends BlameCommand {

  private static final Logger LOG = LoggerFactory.getLogger(TfsBlameCommand.class);
  private static final Pattern LINE_PATTERN = Pattern.compile("([^\t]++)\t([^\t]++)\t([^\t]++)");

  private final TfsConfiguration conf;
  private final File executable;

  public TfsBlameCommand(TfsConfiguration conf, TempFolder temp) {
    this(conf, extractExecutable(temp));
  }

  @VisibleForTesting
  public TfsBlameCommand(TfsConfiguration conf, File executable) {
    this.conf = conf;
    this.executable = executable;
  }

  @Override
  public void blame(BlameInput input, BlameOutput output) {
    Process process = null;
    try {
      LOG.debug("Executing the TFS blame command: " + executable.getAbsolutePath());
      process = new ProcessBuilder(executable.getAbsolutePath()).start();

      OutputStreamWriter stdin = new OutputStreamWriter(process.getOutputStream(), Charsets.UTF_8);
      BufferedReader stdout = new BufferedReader(new InputStreamReader(process.getInputStream(), Charsets.UTF_8));

      stdout.readLine();
      stdin.write(conf.username() + "\r\n");
      stdin.write(conf.password() + "\r\n");
      stdin.flush();
      stdout.readLine();

      for (InputFile inputFile : input.filesToBlame()) {
        LOG.debug("TFS annotating: " + inputFile.absolutePath());

        stdin.write(inputFile.absolutePath() + "\r\n");
        stdin.flush();

        String path = stdout.readLine();
        if (!inputFile.absolutePath().equals(path)) {
          throw new IllegalStateException("Expected the file paths to match: " + inputFile.absolutePath() + " and " + path);
        }

        String linesAsString = stdout.readLine();
        if (linesAsString == null) {
          break;
        }
        int lines = Integer.parseInt(linesAsString, 10);

        if (lines == 0) {
          String reason = stdout.readLine();
          LOG.info(reason);
          continue;
        }

        List<BlameLine> result = Lists.newArrayList();
        for (int i = 0; i < lines; i++) {
          String line = stdout.readLine();

          Matcher matcher = LINE_PATTERN.matcher(line);
          if (!matcher.find()) {
            throw new IllegalStateException("Invalid output from the TFS annotate command: \"" + line + "\" on file: " + path + " at line " + (i + 1));
          }

          String revision = matcher.group(1).trim();
          String author = matcher.group(2).trim();
          String dateStr = matcher.group(3).trim();

          Date date = new Date(Long.parseLong(dateStr, 10));

          result.add(new BlameLine().date(date).revision(revision).author(author));
        }

        if (result.size() == inputFile.lines() - 1) {
          // SONARPLUGINS-3097 TFS do not report blame on last empty line
          result.add(result.get(result.size() - 1));
        }

        output.blameResult(inputFile, result);
      }

      stdin.close();

      int exitCode = process.waitFor();
      if (exitCode != 0) {
        throw new IllegalStateException("The TFS blame command " + executable.getAbsolutePath() + " failed with exit code " + exitCode);
      }
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
