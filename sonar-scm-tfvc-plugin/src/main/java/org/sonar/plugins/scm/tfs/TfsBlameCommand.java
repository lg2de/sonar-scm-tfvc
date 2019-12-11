/*
 * SonarQube :: SCM :: TFVC :: Plugin
 * Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
 *
 * Licensed under the MIT License. See License.txt in the project root for license information.
 */
package org.sonar.plugins.scm.tfs;

import com.google.common.annotations.VisibleForTesting;
import com.google.common.collect.Lists;
import com.google.common.io.Closeables;
import com.google.common.io.Files;
import com.google.common.io.Resources;
import java.io.BufferedReader;
import java.io.File;
import java.io.IOException;
import java.io.InputStream;
import java.io.InputStreamReader;
import java.io.OutputStreamWriter;
import java.nio.charset.StandardCharsets;
import java.util.Date;
import java.util.List;
import java.util.regex.Matcher;
import java.util.regex.Pattern;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.sonar.api.batch.fs.InputFile;
import org.sonar.api.batch.scm.BlameCommand;
import org.sonar.api.batch.scm.BlameLine;
import org.sonar.api.utils.TempFolder;

public class TfsBlameCommand extends BlameCommand {

  private static final String LOG_FORMAT = "{}: {}";
  private static final String LOG_PREFIX = "SCM-TFVC";
  private static final Logger LOG = LoggerFactory.getLogger(TfsBlameCommand.class);
  private static final Pattern LINE_PATTERN = Pattern.compile("([^\t]++)\t([^\t]++)\t([^\t]++)");

  private final TfsConfiguration conf;
  private final File executable;

  public TfsBlameCommand(TfsConfiguration conf, TempFolder temp) {
    this(conf, extractExecutable(temp));
  }

  @VisibleForTesting
  public TfsBlameCommand(TfsConfiguration conf, File executable) {
    logDebug("started blaming with executable %s", executable.getAbsolutePath());
    if (conf.collectionUri().isEmpty()) {
      logError("Missing configuration for CollectionUri");
      throw new IllegalArgumentException("Missing configuration for CollectionUri.");
    }

    this.conf = conf;
    this.executable = executable;
  }

  @Override
  public void blame(BlameInput input, BlameOutput output) {
    Process process = null;
    try {
      logDebug("Executing the TFVC annotate command: %s", executable.getAbsolutePath());
      ProcessBuilder processBuilder = new ProcessBuilder(executable.getAbsolutePath());
      process = processBuilder.start();
      OutputStreamWriter stdin = new OutputStreamWriter(process.getOutputStream(), StandardCharsets.UTF_8);
      BufferedReader stdout = new BufferedReader(new InputStreamReader(process.getInputStream(), StandardCharsets.UTF_8));
      BufferedReader stderr = new BufferedReader(new InputStreamReader(process.getErrorStream(), StandardCharsets.UTF_8));

      String blameOutput = stdout.readLine();
      for (int i=0; i<10; i++) {
        logOutput(blameOutput);
        if (blameOutput != null && !blameOutput.isEmpty()) {
          break;
        }

        Thread.sleep(100);
      }

      if (blameOutput == null || blameOutput.isEmpty()) {
        logError("missing initial output from annotator.");
        return;
      }

      stdin.write(conf.username() + "\r\n");
      stdin.write(conf.password() + "\r\n");
      stdin.write(conf.pat() + "\r\n");
      stdin.flush();

      blameOutput = stdout.readLine();
      logOutput(blameOutput);
      stdin.write(conf.collectionUri() + "\r\n");
      stdin.flush();

      blameOutput = stdout.readLine();
      if (blameOutput.equals("AnnotationFailedOnProject")) {
        logError(stderr.readLine());
        return;
      }

      logOutput(blameOutput);
      for (InputFile inputFile : input.filesToBlame()) {
    	// extract full path from URI, skipping leading slash
    	String fileName = inputFile.uri().getPath().substring(1);
        logInfo("annotating %s", fileName);

        stdin.write(fileName + "\r\n");
        stdin.flush();

        String path = stdout.readLine();
        if (!fileName.equals(path)) {
          throw new IllegalStateException("Expected the file paths to match: " + fileName + " and " + path);
        }

        String linesAsString = stdout.readLine();
        if (linesAsString.equals("AnnotationFailedOnFile")) {
          logError(stderr.readLine());
          continue;
        }

        if (linesAsString == null||linesAsString.equals("AnnotationFailedOnProject")) {
          logError(stderr.readLine());
          break;
        }

        int lines = Integer.parseInt(linesAsString, 10);
        List<BlameLine> result = Lists.newArrayList();
        for (int i = 0; i < lines; i++) {
          String line = stdout.readLine();

          Matcher matcher = LINE_PATTERN.matcher(line);
          if (!matcher.find()) {
            throw new IllegalStateException("Invalid output from the TFVC annotate command: \"" + line + "\" on file: " + path + " at line " + (i + 1));
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
        captureErrorStream(process);
      }

      stdin.close();

      int exitCode = process.waitFor();
      if (exitCode != 0) {
        throw new IllegalStateException("The TFVC annotate command " + executable.getAbsolutePath() + " failed with exit code " + exitCode);
      }
    } catch (IOException e) {
      logError("IOException thrown in the TFVC annotate command: %s", e.getMessage());
    } catch (InterruptedException e) {
      logError("InterruptedException thrown in the TFVC annotate command: %s", e.getMessage());
      // Restore interrupted state...
      Thread.currentThread().interrupt();
    } catch (IllegalStateException e) {
      logError("IllegalStateException thrown in the TFVC annotate command: %s", e.getMessage());
    } finally {
      if (process != null) {
        captureErrorStream(process);
        Closeables.closeQuietly(process.getInputStream());
        Closeables.closeQuietly(process.getOutputStream());
        Closeables.closeQuietly(process.getErrorStream());
        process.destroy();
      }
    }
  }

  private static void logInfo(String message, Object... arguments) {
    String fullMessage = String.format(message, arguments);
	  LOG.info(LOG_FORMAT, LOG_PREFIX, fullMessage);
  }

  private static void logDebug(String message, Object... arguments) {
    String fullMessage = String.format(message, arguments);
    LOG.debug(LOG_FORMAT, LOG_PREFIX, fullMessage);
  }

  private static void logError(String message, Object... arguments) {
    String fullMessage = String.format(message, arguments);
    LOG.error(LOG_FORMAT, LOG_PREFIX, fullMessage);
  }

  private static void logOutput(String output) {
    logDebug("received output: <%s>", output);
  }

  private static void captureErrorStream(Process process) {
    try {
      InputStream errorStream = process.getErrorStream();
      BufferedReader errStream = new BufferedReader(new InputStreamReader(errorStream, StandardCharsets.UTF_8));
      int readBytesCount = errorStream.available();
      char[] errorChars = new char[readBytesCount];

      if (readBytesCount > 0) {
        errStream.read(errorChars);
        String errorString = new String(errorChars);
        if (!errorString.isEmpty()) {
          logError(errorString);
        }
      }
    } catch (IOException e) {
      logError("Exception thrown while getting error Stream data: %s", e);
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
