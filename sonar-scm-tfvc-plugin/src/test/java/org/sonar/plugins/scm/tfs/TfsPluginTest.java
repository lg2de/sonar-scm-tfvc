/*
 * SonarQube :: SCM :: TFVC :: Plugin
 * Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
 *
 * Licensed under the MIT License. See License.txt in the project root for license information.
 */
package org.sonar.plugins.scm.tfs;

import org.junit.Test;
import org.sonar.api.Plugin.Context;

import static org.fest.assertions.Assertions.assertThat;

public class TfsPluginTest {

  @Test
  public void getExtensions() {
    Context cntx = new Context(null);
    new TfsPlugin().define(cntx);
    assertThat(cntx.getExtensions()).hasSize(3 + TfsConfiguration.getProperties().size());
  }

}
