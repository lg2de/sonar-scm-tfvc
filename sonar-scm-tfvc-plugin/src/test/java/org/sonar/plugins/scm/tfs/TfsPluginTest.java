/*
 * SonarQube :: SCM :: TFVC :: Plugin
 * Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
 *
 * Licensed under the MIT License. See License.txt in the project root for license information.
 */
package org.sonar.plugins.scm.tfs;

import org.junit.Test;
import org.sonar.api.Plugin.Context;
import org.sonar.api.SonarQubeSide;
import org.sonar.api.SonarRuntime;
import org.sonar.api.internal.SonarRuntimeImpl;
import org.sonar.api.utils.Version;

import static org.fest.assertions.Assertions.assertThat;

public class TfsPluginTest {

  @Test
  public void getExtensions() {
    SonarRuntime runtime = SonarRuntimeImpl.forSonarQube(Version.create(7, 4), SonarQubeSide.SCANNER);
    Context context = new Context(runtime);
    new TfsPlugin().define(context);
    assertThat(context.getExtensions()).hasSize(3 + TfsConfiguration.getProperties().size());
  }

}
