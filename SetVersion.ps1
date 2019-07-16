[CmdletBinding()]

param(
  [Parameter(Mandatory=$True, position=0)]
  [int]$majorVersion,
  [Parameter(Mandatory=$True, position=1)]
  [int]$minorVersion,
  [Parameter(Mandatory=$True, position=2)]
  [int]$buildVersion
)

$ErrorActionPreference = "Stop"

$pomVersion = "$majorVersion.$minorVersion"
if ($buildVersion -gt 0) {
  $pomVersion = "$pomVersion.$buildVersion"
}
if ($buildVersion -lt 0) {
  $pomVersion = "$pomVersion-SNAPSHOT"
}

$assemblyVersion = "$majorVersion.$minorVersion"
if ($buildVersion -gt 0) {
  $assemblyVersion = "$assemblyVersion.$buildVersion.0"
} else {
  $assemblyVersion = "$assemblyVersion.0.0"
}

function UpdatePOMVersion($file) {
  [xml]$content = Get-Content $file
  #$newContent = $content -replace "(<project(.|\r|\n)*)<version>\d+\.\d+[^<]*</version>","$1<version>$pomVersion</version>"
  #Set-Content $file $newContent
  if ($content.project.version) {
    $content.project.version = $pomVersion
  } else {
  	$content.project.parent.version = $pomVersion
  }
  $content.Save($file)
}

function UpdateAssemblyVersion($file) {
  $content = Get-Content $file
  $newContent = $content -replace "AssemblyVersion\(.*\)","AssemblyVersion(`"$assemblyVersion`")"
  Set-Content $file $newContent
}

Write-Host "setting version $pomVersion"

UpdatePOMVersion "$PSScriptRoot\pom.xml"
UpdatePOMVersion "$PSScriptRoot\sonar-scm-tfvc-plugin\pom.xml"
UpdatePOMVersion "$PSScriptRoot\SonarTfsAnnotate\pom.xml"

Write-Host "setting version $assemblyVersion"
UpdateAssemblyVersion "$PSScriptRoot\SonarTfsAnnotate\Properties\AssemblyInfo.cs"
