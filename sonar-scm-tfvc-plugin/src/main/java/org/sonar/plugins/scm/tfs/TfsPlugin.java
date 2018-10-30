/*
 * SonarQube :: SCM :: TFVC :: Plugin
 * Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
 *
 * Licensed under the MIT License. See License.txt in the project root for license information.
 */
package org.sonar.plugins.scm.tfs;

import org.sonar.api.Plugin;

public class TfsPlugin implements Plugin {
  @Override
  public void define(Context context) {
    context.addExtensions(
      TfsScmProvider.class,
      TfsBlameCommand.class
    );
    context.addExtensions(TfsConfiguration.getPropertyDefinitions());
  }
}
