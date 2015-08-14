/*
 * SonarQube :: SCM :: TFVC :: Plugin
 * Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
 *
 * Licensed under the MIT License. See License.txt in the project root for license information.
 */
package org.sonar.plugins.scm.tfs;

import com.google.common.base.Strings;
import com.google.common.collect.ImmutableList;
import org.sonar.api.BatchComponent;
import org.sonar.api.CoreProperties;
import org.sonar.api.PropertyType;
import org.sonar.api.batch.InstantiationStrategy;
import org.sonar.api.config.PropertyDefinition;
import org.sonar.api.config.Settings;
import org.sonar.api.resources.Qualifiers;

import java.util.List;

@InstantiationStrategy(InstantiationStrategy.PER_BATCH)
public class TfsConfiguration implements BatchComponent {

  private static final String CATEGORY = "TFVC";
  private static final String USERNAME_PROPERTY_KEY = "sonar.tfvc.username";
  private static final String PASSWORD_PROPERTY_KEY = "sonar.tfvc.password.secured";
  private final Settings settings;

  public TfsConfiguration(Settings settings) {
    this.settings = settings;
  }

  public static List<PropertyDefinition> getProperties() {
    return ImmutableList.of(
      PropertyDefinition.builder(USERNAME_PROPERTY_KEY)
        .name("Username")
        .description("Username to be used for TFVC authentication")
        .type(PropertyType.STRING)
        .onQualifiers(Qualifiers.PROJECT)
        .category(CoreProperties.CATEGORY_SCM)
        .subCategory(CATEGORY)
        .index(0)
        .build(),
      PropertyDefinition.builder(PASSWORD_PROPERTY_KEY)
        .name("Password")
        .description("Password to be used for TFVC authentication")
        .type(PropertyType.PASSWORD)
        .onQualifiers(Qualifiers.PROJECT)
        .category(CoreProperties.CATEGORY_SCM)
        .subCategory(CATEGORY)
        .index(1)
        .build());
  }

  public String username() {
    return Strings.nullToEmpty(settings.getString(USERNAME_PROPERTY_KEY));
  }

  public String password() {
    return Strings.nullToEmpty(settings.getString(PASSWORD_PROPERTY_KEY));
  }

}
