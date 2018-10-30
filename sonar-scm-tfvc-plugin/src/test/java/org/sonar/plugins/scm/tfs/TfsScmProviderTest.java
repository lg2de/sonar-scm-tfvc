/*
 * SonarQube :: SCM :: TFVC :: Plugin
 * Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
 *
 * Licensed under the MIT License. See License.txt in the project root for license information.
 */
package org.sonar.plugins.scm.tfs;

import org.junit.Rule;
import org.junit.Test;
import org.junit.rules.ExpectedException;
import org.junit.rules.TemporaryFolder;

import java.io.File;
import java.io.IOException;

import static org.assertj.core.api.Assertions.assertThat;

public class TfsScmProviderTest {

    @Rule
    public TemporaryFolder temp = new TemporaryFolder();
    @Rule
    public ExpectedException thrown = ExpectedException.none();

    @Test
    public void sanityCheck() {
        assertThat(new TfsScmProvider(null).key()).isEqualTo("tfvc");
    }

    @Test
    public void testAutodetection() throws IOException {
        File baseDirEmpty = temp.newFolder();
        assertThat(new TfsScmProvider(null).supports(baseDirEmpty)).isFalse();

        File tfsBaseDir = temp.newFolder();
        new File(tfsBaseDir, "$tf").mkdir();
        assertThat(new TfsScmProvider(null).supports(tfsBaseDir)).isTrue();
    }

}
