/*
 * SonarQube :: SCM :: TFVC :: Plugin
 * Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
 *
 * Licensed under the MIT License. See License.txt in the project root for license information.
 */
package org.sonar.plugins.scm.tfs.helpers;

import ch.qos.logback.classic.Level;
import ch.qos.logback.classic.spi.ILoggingEvent;
import ch.qos.logback.core.ConsoleAppender;
import java.util.ArrayList;
import java.util.List;

public class TestAppender extends ConsoleAppender<ILoggingEvent> {

  private final List<String> errorEvents = new ArrayList<>();

  private final List<String> warningEvents = new ArrayList<>();

  @Override
  public void doAppend(ILoggingEvent event) {
    if (event.getLevel() == Level.ERROR) {
      errorEvents.add(event.getFormattedMessage());
    }
    if (event.getLevel() == Level.WARN) {
      warningEvents.add(event.getFormattedMessage());
    }
  }

  public List<String> getErrorEvents() {
    return errorEvents;
  }

  public List<String> getWarningEvents() {
    return warningEvents;
  }

}
