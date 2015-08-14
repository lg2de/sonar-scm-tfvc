/*
 * SonarQube :: SCM :: TFVC :: Plugin
 * Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
 *
 * Licensed under the MIT License. See License.txt in the project root for license information.
 */
package org.sonar.plugins.scm.tfs;

import org.sonar.api.batch.scm.BlameCommand;
import org.sonar.api.batch.scm.ScmProvider;

import java.io.File;

public class TfsScmProvider extends ScmProvider {

  private final TfsBlameCommand blameCommand;

  public TfsScmProvider(TfsBlameCommand blameCommand) {
    this.blameCommand = blameCommand;
  }

  @Override
  public String key() {
    return "tfvc";
  }

  @Override
  public boolean supports(File baseDir) {
    return new File(baseDir, "$tf").exists();
  }

  @Override
  public BlameCommand blameCommand() {
    return this.blameCommand;
  }

}
