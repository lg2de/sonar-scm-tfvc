/*
 * SonarQube :: SCM :: TFVC :: Plugin
 * Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
 *
 * Licensed under the MIT License. See License.txt in the project root for license information.
 */
package org.sonar.plugins.scm.tfs;

import com.google.common.collect.ImmutableList;
import org.sonar.api.SonarPlugin;

import java.util.List;

public class TfsPlugin extends SonarPlugin {

  @Override
  public List getExtensions() {
    ImmutableList.Builder builder = ImmutableList.builder();

    builder.add(
      TfsScmProvider.class,
      TfsBlameCommand.class,
      TfsConfiguration.class);

    builder.addAll(TfsConfiguration.getProperties());

    return builder.build();
  }

}
