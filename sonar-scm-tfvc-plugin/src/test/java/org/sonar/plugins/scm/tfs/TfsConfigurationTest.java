/*
 * SonarQube :: SCM :: TFVC :: Plugin
 * Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
 *
 * Licensed under the MIT License. See License.txt in the project root for license information.
 */
package org.sonar.plugins.scm.tfs;

import org.junit.Test;
import org.sonar.api.config.PropertyDefinitions;
import org.sonar.api.config.Settings;

import static org.fest.assertions.Assertions.assertThat;

public class TfsConfigurationTest {

  @Test
  public void sanityCheck() {
    Settings settings = new Settings(new PropertyDefinitions(TfsConfiguration.getProperties()));
    TfsConfiguration config = new TfsConfiguration(settings);

    assertThat(config.username()).isEmpty();
    assertThat(config.password()).isEmpty();
    assertThat(config.collectionUri()).isEmpty();

    settings.setProperty("sonar.tfvc.username", "foo");
    assertThat(config.username()).isEqualTo("foo");

    settings.setProperty("sonar.tfvc.password.secured", "pwd");
    assertThat(config.password()).isEqualTo("pwd");

    settings.setProperty("sonar.tfvc.collectionuri", "uri");
    assertThat(config.collectionUri()).isEqualTo("uri");
  }

}
