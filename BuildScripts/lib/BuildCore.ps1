#!/usr/bin/env pwsh
#Requires -Version 5.0

# Imports
. (Join-Path $PSScriptRoot "Constants.ps1")
. (Join-Path $PSScriptRoot "Enums.ps1")
. (Join-Path $PSScriptRoot "FileTools.ps1")
. (Join-Path $PSScriptRoot "MoveTools.ps1")
. (Join-Path $PSScriptRoot "TFMTools.ps1")
. (Join-Path $PSScriptRoot "EngineMap.ps1")

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

    $Private:RootDirectory = (Join-Path $ClientCompiledTarget $Game $TargetFramework (&{If($RuntimeIdentifier -ne "") {$RuntimeIdentifier} Else {$Null}}))
    $Private:ResourcesDirectory = (Join-Path $Private:RootDirectory "Resources")
    $Private:CommonLibsDirectory = (Join-Path $Private:ResourcesDirectory "Binaries")
    $Private:SpecialLibsDirectory = (Join-Path $Private:CommonLibsDirectory $Private:SpecialName)

    $Private:BuildTargetDirectory = $SkipMoveLibraries ? $Private:SpecialLibsDirectory : (Join-Path $Private:SpecialLibsDirectory "Source")

    $Private:DotnetArgs = @(
      "publish"
      $ClientProjectPath
      "--framework:$TargetFramework"
      "--output:$Private:BuildTargetDirectory" + [IO.Path]::DirectorySeparatorChar
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
    $Private:tmp = (Join-Path $Private:BuildTargetDirectory "client$Private:ClientSuffix")
    if ($Engine -eq 'UniversalGL' -and $RuntimeIdentifier -eq "any") {
      # cross platform net6.0, net7.0
      # no startup executable (run with 'dotnet')
      $Private:tmp += '.nomatches'
    }elseif (Test-Path "$Private:tmp.") {
      # linux based
      $Private:tmp += '.'
    }
    elseif (Test-Path "$Private:tmp.exe") {
      # WinForms net6.0-windows, net7.0-windows or UniversalGL Windows specific net7.0 win10-x64, ...
      $Private:tmp += '.exe'
    }
    elseif (Test-Path "$Private:tmp.dll") {
      # net48, net6.0 android-arm64, ...
      $Private:tmp += '.*'
    }
    if (Test-Path $Private:tmp)
    {
      Get-ChildItem $Private:tmp | ForEach-Object {
        Move-ClientBinaries $_ $Private:ResourcesDirectory
      }
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