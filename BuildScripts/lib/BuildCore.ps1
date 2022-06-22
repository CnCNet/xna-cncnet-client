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
    [Switch]
    $SkipMoveLibraries
  )

  begin {
    Write-Host
    Write-Host "Building $Game for $Engine ($Configuration)..." -ForegroundColor Blue
    Write-Host

    $Private:TargetFrameworkWithoutTFM = Get-TargetFrameworkWithoutTFM $TargetFramework
    $Private:SpecialName = Get-PlatformName $Engine
    $Private:ClientSuffix = Get-Suffix $Engine

    $Private:RootDirectory = "$ClientCompiledTarget\$Game\$Private:TargetFrameworkWithoutTFM"
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
    )

    Write-Debug ""
    Write-Debug "Invoke Build-Project"
    Write-Debug ""
    Write-Debug "Game: $Game; Engine: $Engine; Configuration: $Configuration; TargetFramework: $TargetFramework"
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
      throw "Failed to build project"
    }
  }

  end {
    $Private:tmp = "$Private:BuildTargetDirectory\client$Private:ClientSuffix."
    if ($Private:TargetFrameworkWithoutTFM.Contains('.')) {
      # netcoreapp3.0, netcoreapp3.1, net5.0 net6.0, net7.0
      # move exe only
      $Private:tmp += 'exe'
    }
    else {
      # net40, net45, net46, net47, net48
      # move exe, pdb, app.config
      $Private:tmp += '*'
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
    Write-Host "Success to Building $Game for $Engine ($Configuration)..." -ForegroundColor Green
    Write-Host
  }
}