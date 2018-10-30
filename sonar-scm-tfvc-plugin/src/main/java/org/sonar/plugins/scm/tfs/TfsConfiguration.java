/*
 * SonarQube :: SCM :: TFVC :: Plugin
 * Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
 *
 * Licensed under the MIT License. See License.txt in the project root for license information.
 */
package org.sonar.plugins.scm.tfs;

import com.google.common.collect.ImmutableList;
import org.sonar.api.CoreProperties;
import org.sonar.api.PropertyType;
import org.sonar.api.batch.InstantiationStrategy;
import org.sonar.api.batch.ScannerSide;
import org.sonar.api.config.Configuration;
import org.sonar.api.config.PropertyDefinition;
import org.sonar.api.resources.Qualifiers;

import java.util.List;

@ScannerSide
@InstantiationStrategy(InstantiationStrategy.PER_BATCH)
public class TfsConfiguration {

  private static final String CATEGORY = "TFVC";
  private static final String USERNAME_PROPERTY_KEY = "sonar.tfvc.username";
  private static final String PASSWORD_PROPERTY_KEY = "sonar.tfvc.password.secured";
  private static final String COLLECTIONURI_PROPERTY_KEY = "sonar.tfvc.collectionuri";
  private static final String PAT_PROPERTY_KEY = "sonar.tfvc.pat.secured";
  private final Configuration configuration;

  public TfsConfiguration(Configuration configuration) {
    this.configuration = configuration;
  }

  static List<PropertyDefinition> getPropertyDefinitions() {
    return ImmutableList.of(
      PropertyDefinition.builder(PAT_PROPERTY_KEY)
        .name("PersonalAccessToken")
        .description("All scopes PAT when connecting to Visual Studio Team Services")
        .type(PropertyType.PASSWORD)
        .onQualifiers(Qualifiers.PROJECT)
        .category(CoreProperties.CATEGORY_SCM)
        .subCategory(CATEGORY)
        .index(0)
        .build(),
      PropertyDefinition.builder(USERNAME_PROPERTY_KEY)
        .name("Username")
        .description("Username when connecting to on-premises Team Foundation Server")
        .type(PropertyType.STRING)
        .onQualifiers(Qualifiers.PROJECT)
        .category(CoreProperties.CATEGORY_SCM)
        .subCategory(CATEGORY)
        .index(1)
        .build(),
      PropertyDefinition.builder(PASSWORD_PROPERTY_KEY)
        .name("Password")
        .description("Password when connecting to on-premises Team Foundation Server")
        .type(PropertyType.PASSWORD)
        .onQualifiers(Qualifiers.PROJECT)
        .category(CoreProperties.CATEGORY_SCM)
        .subCategory(CATEGORY)
        .index(2)
        .build(),
      PropertyDefinition.builder(COLLECTIONURI_PROPERTY_KEY)
        .name("CollectionURI")
        .description("Example: - https://[account].visualstudio.com/DefaultCollection or http://ServerName:8080/tfs/DefaultCollection")
        .type(PropertyType.STRING)
        .onQualifiers(Qualifiers.PROJECT)
        .category(CoreProperties.CATEGORY_SCM)
        .subCategory(CATEGORY)
        .index(3)
        .build());
  }

  public String username() {
    return configuration.get(USERNAME_PROPERTY_KEY).orElse("");
  }

  public String password() {
    return configuration.get(PASSWORD_PROPERTY_KEY).orElse("");
  }

  public String collectionUri() {
    return configuration.get(COLLECTIONURI_PROPERTY_KEY).orElse("");
  }

  public String pat() {
    return configuration.get(PAT_PROPERTY_KEY).orElse("");
  }

}
