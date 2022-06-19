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
    [bool]
    $SkipMoveCommonLibraries
  )

  begin {
    $Private:TargetFrameworkWithoutTFM = Get-TargetFrameworkWithoutTFM $TargetFramework
    $Private:SpecialName = Get-PlatformName $Engine
    $Private:ClientSuffix = Get-Suffix $Engine

    $Private:DotnetArgs = @(
      "build"
      $ClientProjectPath
      "--framework:$TargetFramework"
      "--output:$ClientCompiledTarget\$Game\$Private:TargetFrameworkWithoutTFM\Binaries\$Private:SpecialName\"
      "--no-self-contained"
      "--configuration:$Configuration"
      "-p:Engine=$Engine"
      "-p:Game=$Game"
    )

    Write-Host
    Write-Host "Building $Game for $Engine ($Configuration)..." -ForegroundColor Green
    Write-Host
    Write-Debug "Dotnet args: $Private:DotnetArgs"
  }

  process {
    dotnet $Private:DotnetArgs
    if ($LASTEXITCODE -ne 0) {
      throw "Failed to build project"
    }
  }

  end {
    Get-ChildItem $ClientCompiledTarget\$Game\$Private:TargetFrameworkWithoutTFM\Binaries\$Private:SpecialName\DXMainClient.* | ForEach-Object {
      Move-ClientBinaries $_ "$ClientCompiledTarget\$Game\$Private:TargetFrameworkWithoutTFM" $Private:ClientSuffix
    }
    if (!$SkipMoveCommonLibraries) {
      Move-CommonLibraries "$ClientCompiledTarget\$Game\$Private:TargetFrameworkWithoutTFM\Binaries\$Private:SpecialName\"
    }
  }
}