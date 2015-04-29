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

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.sonar.api.batch.scm.BlameLine;

import java.io.BufferedReader;
import java.io.IOException;
import java.text.DateFormat;
import java.text.ParseException;
import java.text.SimpleDateFormat;
import java.util.ArrayList;
import java.util.Date;
import java.util.List;
import java.util.Locale;
import java.util.regex.Matcher;
import java.util.regex.Pattern;

public class TfsBlameConsumer {

  private static final Logger LOG = LoggerFactory.getLogger(TfsBlameConsumer.class);

  private static final String TFS_TIMESTAMP_PATTERN = "MM/dd/yyyy";

  /* 3 username 3/13/2006 line */
  // TODO simplify

  private static final Pattern LINE_PATTERN = Pattern.compile("([^ ]+)[ ]+([^ ]+)[ ]+([^ ]+)");

  private final List<BlameLine> lines = new ArrayList<BlameLine>();

  private final DateFormat format = new SimpleDateFormat(TFS_TIMESTAMP_PATTERN);

  private final String filename;

  public TfsBlameConsumer(String filename) {
    this.filename = filename;
  }

  public void process(BufferedReader stdout) throws IOException {
    String line;
    while ((line = stdout.readLine()) != null) {
      if (line.startsWith("local") || line.startsWith("unknow")) {
        throw new IllegalStateException("Unable to blame file " + filename + ". No blame info at line " + (getLines().size() + 1) + ". Is file commited?\n [" + line + "]");
      }
      Matcher matcher = LINE_PATTERN.matcher(line);
      if (matcher.find()) {
        String revision = matcher.group(1).trim();
        String author = matcher.group(2).trim();
        String dateStr = matcher.group(3).trim();

        Date date = parseDate(dateStr);

        lines.add(new BlameLine().date(date).revision(revision).author(author));
      }
    }
  }

  /**
   * Converts the date timestamp from the output into a date object.
   *
   * @return A date representing the timestamp of the log entry.
   */
  protected Date parseDate(String date) {
    try {
      return format.parse(date);
    } catch (ParseException e) {
      LOG.warn(
        "skip ParseException: " + e.getMessage() + " during parsing date " + date
          + " with pattern " + TFS_TIMESTAMP_PATTERN + " with Locale " + Locale.ENGLISH, e);
      return null;
    }
  }

  public List<BlameLine> getLines() {
    return lines;
  }
}
