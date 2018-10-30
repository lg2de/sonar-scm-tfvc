/*
 * SonarQube :: SCM :: TFVC :: Plugin
 * Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
 *
 * Licensed under the MIT License. See License.txt in the project root for license information.
 */
package org.sonar.plugins.scm.tfs;

import org.junit.Test;
import org.sonar.api.config.PropertyDefinitions;
import org.sonar.api.config.internal.MapSettings;

import static org.assertj.core.api.Assertions.assertThat;


public class TfsConfigurationTest {

    @Test
    public void sanityCheck() {
        MapSettings settings = new MapSettings(new PropertyDefinitions(TfsConfiguration.getPropertyDefinitions()));
        TfsConfiguration config = new TfsConfiguration(settings.asConfig());

        assertThat(config.username()).isEmpty();
        assertThat(config.password()).isEmpty();
        assertThat(config.collectionUri()).isEmpty();
        assertThat(config.pat()).isEmpty();

        settings.setProperty("sonar.tfvc.username", "foo");
        assertThat(config.username()).isEqualTo("foo");

        settings.setProperty("sonar.tfvc.password.secured", "pwd");
        assertThat(config.password()).isEqualTo("pwd");

        settings.setProperty("sonar.tfvc.collectionuri", "uri");
        assertThat(config.collectionUri()).isEqualTo("uri");

        settings.setProperty("sonar.tfvc.pat.secured", "pat");
        assertThat(config.pat()).isEqualTo("pat");
    }

}
