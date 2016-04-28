[![Build status](https://ci.appveyor.com/api/projects/status/s2ko8rmp4iy9gq5n/branch/master?svg=true)](https://ci.appveyor.com/project/SonarSource/sonar-scm-tfvc/branch/master)

## SonarQube SCM TFVC plugin
### Description
Implements SCM dependent features of SonarQube for [Microsoft Team Foundation Server](http://en.wikipedia.org/wiki/Team_Foundation_Server)'s own Version Control (all versions).
It requires analysis to be executed from Windows machines with the Team Foundation Server Object Model installed ([download for TFS 2013](https://visualstudiogallery.msdn.microsoft.com/3278bfa7-64a7-4a75-b0da-ec4ccb8d21b6)). Additionally, for working with Team Foundation Server 2015 the user also needs to specify the Team Foundation Server Collection URI.
### Usage
Auto-detection of the SCM provider will work if there is a "$tf" folder in the project root directory. Otherwise you can force it by setting the "sonar.scm.provider" property to "tfvc".
For interacting with Team Foundation Server 2015 users need to enter the collection URI corresponding to the TFVC collection of the project. This property can be set and edited either through sonar-runner properties or the SonarQube Server.
Optionally, you can configure additional properties:

|Key|Description|Required|Default value|
|sonar.tfvc.username|Username to be used for TFVC authentication.|Optional for Windows authentication or if already cached.|None|
|sonar.tfvc.password.secured|Password to be used for TFVC authentication.|Optional for Windows authentication or if already cached.|None|
|sonar.tfvc.collectionuri|URI corresponding to the TFVC collection of the project|Mandatory for working with Team Foundation Server 2015|None|
### Known Limitations
The annotation does not see through merging and branching changesets (See [SONARTFVC-7](https://jira.sonarsource.com/browse/SONARTFVC-7))
Current version of the plugin does not support hosted builds on Visual Studio Online (VSO) (See [MMF-85](https://jira.sonarsource.com/browse/MMF-85))
