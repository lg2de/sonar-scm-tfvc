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

public class TestAppender extends ConsoleAppender {

  private List<String> errorEvents = new ArrayList<>();

  @Override
  public void doAppend(Object eventObject) {
    ILoggingEvent event = (ILoggingEvent) eventObject;

    if (event.getLevel() == Level.ERROR) {
      errorEvents.add(event.getMessage());
    }
  }

  public List<String> getErrorEvents() {
    return errorEvents;
  }

}
