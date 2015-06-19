/*
 * SonarQube :: SCM :: TFVC :: Plugin
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

    settings.setProperty("sonar.tfvc.username", "foo");
    assertThat(config.username()).isEqualTo("foo");

    settings.setProperty("sonar.tfvc.password.secured", "pwd");
    assertThat(config.password()).isEqualTo("pwd");
  }

}
