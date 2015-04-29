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
    return "tfs";
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
