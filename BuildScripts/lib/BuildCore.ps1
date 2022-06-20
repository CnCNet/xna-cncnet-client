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
    $Private:RootDirectory = "$ClientCompiledTarget\$Game\$Private:TargetFrameworkWithoutTFM\Resources"
    $Private:BuildTargetDirectory = "$Private:RootDirectory\Binaries\$Private:SpecialName"

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
      Move-ClientBinaries $_ $Private:RootDirectory
    }
    if (!$SkipMoveCommonLibraries) {
      Move-CommonLibraries "$Private:BuildTargetDirectory"
    }
  }
}