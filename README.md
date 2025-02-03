[![Build status](https://ci.appveyor.com/api/projects/status/pbv57p4drgrcaydn/branch/main?svg=true)](https://ci.appveyor.com/project/lg2de/sonar-scm-tfvc/branch/main)
[![Sonarcloud Status](https://sonarcloud.io/api/project_badges/measure?project=lg2de_sonar-scm-tfvc&metric=alert_status)](https://sonarcloud.io/dashboard?id=lg2de_sonar-scm-tfvc)

## SonarQube SCM TFVC plugin
### Description
Implements SCM dependent features of SonarQube for [Microsoft Azure DevOps Server/Services](https://en.wikipedia.org/wiki/Azure_DevOps_Server)'s own Version Control (all versions).
It requires analysis to be executed from Windows machines.

### Usage
Auto-detection of the SCM provider will work if there is a "$tf" folder in the project root directory.
Otherwise, you can force it by setting the "sonar.scm.provider" property to "tfvc".

For interacting with Azure DevOps Server or Services, users need to enter the collection URI corresponding to the TFVC collection of the project.
This property can be set and edited either through sonar-runner properties or the SonarQube Server.

The authentication is performed either using username and password,
or with a Personal Access Token (PAT).

These properties are available:

| Key                         | Description                                                     | Required                                                    | Default value |
|-----------------------------|-----------------------------------------------------------------|-------------------------------------------------------------|---------------|
| sonar.tfvc.collectionuri    | URI corresponding to the TFVC collection of the project         | Mandatory for working with Azure DevOps Server or Services. | None          |
| sonar.tfvc.username         | Username to be used for TFVC authentication.                    | Optional for Windows authentication or if already cached.   | None          |
| sonar.tfvc.password.secured | Password to be used for TFVC authentication.                    | Optional for Windows authentication or if already cached.   | None          |
| sonar.tfvc.pat.secured      | Personal Access Token (PAT) to be used for TFVC authentication. | Optional for Windows authentication.                        | None          |

Due to changes in SonarQube starting version 9.1, secured parameters cannot be transferred from the server to the plugin.
Therefore, the parameters `sonar.tfvc.pat.secured` (or `sonar.tfvc.password.secured`)
must be specified as commandline parameter when running the analyzer.
