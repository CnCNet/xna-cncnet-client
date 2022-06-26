#!/usr/bin/env pwsh
#Requires -Version 5.0

# Imports
. $PSScriptRoot\Constants.ps1
. $PSScriptRoot\Enums.ps1
. $PSScriptRoot\FileTools.ps1
. $PSScriptRoot\MoveTools.ps1
. $PSScriptRoot\TFMTools.ps1
. $PSScriptRoot\EngineMap.ps1

function Build-Project {
  [CmdletBinding()]
  param (
    [Parameter(Mandatory)]
    [Games]
    $Game,
    [Parameter(Mandatory)]
    [Engines]
    $Engine,
    [Parameter(Mandatory)]
    [Configurations]
    $Configuration,
    [Parameter(Mandatory)]
    [string]
    $TargetFramework,
    [Parameter()]
    [string]
    $RuntimeIdentifier,
    [Parameter()]
    [string]
    $PlatformTarget,
    [Parameter()]
    [Switch]
    $SkipMoveLibraries
  )

  begin {
    Write-Host
    Write-Host "Building $Game $Engine $Configuration $TargetFramework $PlatformTarget $RuntimeIdentifier..." -ForegroundColor Blue
    Write-Host

    $Private:TargetFrameworkWithoutTFM = Get-TargetFrameworkWithoutTFM $TargetFramework
    $Private:SpecialName = Get-PlatformName $Engine
    $Private:ClientSuffix = Get-Suffix $Engine

    $Private:RootDirectory = "$ClientCompiledTarget\$Game\$TargetFramework\" + (&{If($RuntimeIdentifier -ne "") {$RuntimeIdentifier} Else {$Null}})
    $Private:ResourcesDirectory = "$Private:RootDirectory\Resources"
    $Private:CommonLibsDirectory = "$Private:ResourcesDirectory\Binaries"
    $Private:SpecialLibsDirectory = "$Private:CommonLibsDirectory\$Private:SpecialName"

    $Private:BuildTargetDirectory = $SkipMoveLibraries ? $Private:SpecialLibsDirectory : "$Private:SpecialLibsDirectory\Source"

    $Private:DotnetArgs = @(
      "publish"
      $ClientProjectPath
      "--framework:$TargetFramework"
      "--output:$Private:BuildTargetDirectory\"
      "--no-self-contained"
      "--configuration:$Configuration"
      "-p:Engine=$Engine"
      "-p:Game=$Game"
      (&{If($PlatformTarget -ne "") {"-p:PlatformTarget=$PlatformTarget"} Else {$Null}})
      (&{If($RuntimeIdentifier -ne "" -and $RuntimeIdentifier -ne "any") {"--runtime:$RuntimeIdentifier"} Else {$Null}})
    )

    Write-Debug ""
    Write-Debug "Invoke Build-Project"
    Write-Debug ""
    Write-Debug "Game: $Game; Engine: $Engine; Configuration: $Configuration; TargetFramework: $TargetFramework; PlatformTarget: $PlatformTarget RuntimeIdentifier: $RuntimeIdentifier"
    Write-Debug "SkipMoveLibraries: $SkipMoveLibraries; TargetFrameworkWithoutTFM: $Private:TargetFrameworkWithoutTFM; SpecialName: $Private:SpecialName; ClientSuffix: $Private:ClientSuffix"
    Write-Debug "RootDirectory: $Private:RootDirectory"
    Write-Debug "ResourcesDirectory: $Private:ResourcesDirectory"
    Write-Debug "CommonLibsDirectory: $Private:CommonLibsDirectory"
    Write-Debug "SpecialLibsDirectory: $Private:SpecialLibsDirectory"
    Write-Debug "BuildTargetDirectory: $Private:BuildTargetDirectory"
    Write-Debug "DotnetArgs: $Private:DotnetArgs"
  }

  process {
    dotnet $Private:DotnetArgs
    if ($LASTEXITCODE -ne 0) {
      throw "Build failed for $Game $Engine $Configuration $TargetFramework $PlatformTarget $RuntimeIdentifier"
    }
  }

  end {
    $Private:tmp = "$Private:BuildTargetDirectory\client$Private:ClientSuffix"
    if ($TargetFramework.Contains('-windows') -or $RuntimeIdentifier -eq 'any') {
      # net6.0-windows, net7.0-windows
      # move exe only
      $Private:tmp += '.exe'
    }
    elseif ($TargetFramework.Contains('.')) {
      # net6.0, net7.0
      # move binary only
      $Private:tmp += '.'
    }
    else {
      # net48
      # move exe, pdb, app.config
      $Private:tmp += '.*'
    }
    Get-ChildItem $Private:tmp | ForEach-Object {
      Move-ClientBinaries $_ $Private:ResourcesDirectory
    }
    if (!$SkipMoveLibraries) {
      # Move All of the special libraries to special folder.
      Move-Libraries -Path $Private:BuildTargetDirectory -Destination $Private:SpecialLibsDirectory -Special -Engine $Engine
      # Move All of the files to common folder.
      Move-Libraries -Path $Private:BuildTargetDirectory -Destination $Private:CommonLibsDirectory
      # Remove the temp dir.
      Remove-Item $Private:BuildTargetDirectory -Force
    }

    Write-Host
    Write-Host "Build succeeded for $Game $Engine $Configuration $TargetFramework $PlatformTarget $RuntimeIdentifier..." -ForegroundColor Green
    Write-Host
  }
}