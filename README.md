[![Build status](https://ci.appveyor.com/api/projects/status/pbv57p4drgrcaydn/branch/main?svg=true)](https://ci.appveyor.com/project/lg2de/sonar-scm-tfvc/branch/main)
[![Sonarcloud Status](https://sonarcloud.io/api/project_badges/measure?project=lg2de_sonar-scm-tfvc&metric=alert_status)](https://sonarcloud.io/dashboard?id=lg2de_sonar-scm-tfvc)

## SonarQube SCM TFVC plugin
### Description
Implements SCM dependent features of SonarQube for [Microsoft Team Foundation Server](http://en.wikipedia.org/wiki/Team_Foundation_Server)'s own Version Control (all versions).
It requires analysis to be executed from Windows machines.
Additionally, for working with Team Foundation Server 2015 the user also needs to specify the Team Foundation Server Collection URI.

### Usage
Auto-detection of the SCM provider will work if there is a "$tf" folder in the project root directory. Otherwise you can force it by setting the "sonar.scm.provider" property to "tfvc".
For interacting with Team Foundation Server 2015 users need to enter the collection URI corresponding to the TFVC collection of the project. This property can be set and edited either through sonar-runner properties or the SonarQube Server.
Optionally, you can configure additional properties:

| Key | Description | Required | Default value |
| --- | --- | --- | --- |
| sonar.tfvc.collectionuri | URI corresponding to the TFVC collection of the project | Mandatory for working with Team Foundation Server 2015 | None |
| sonar.tfvc.username | Username to be used for TFVC authentication. | Optional for Windows authentication or if already cached. | None |
| sonar.tfvc.password.secured | Password to be used for TFVC authentication. | Optional for Windows authentication or if already cached. | None |
| sonar.tfvc.pat.secured | Personal Access Token (PAT) to be used for TFVC authentication. | Optional for Windows authentication. | None |
