#!/usr/bin/env pwsh
#Requires -Version 5.0

# Imports
. (Join-Path $PSScriptRoot "Constants.ps1")
. (Join-Path $PSScriptRoot "Enums.ps1")
. (Join-Path $PSScriptRoot "BuildCore.ps1")
. (Join-Path $PSScriptRoot "ClearTools.ps1")

# See https://docs.microsoft.com/en-us/dotnet/core/rid-catalog for adding specific RuntimeIdentifiers. Examples:
#Build-Project -Configuration $Configuration -Game YR -Engine UniversalGL -TargetFramework net6.0 -RuntimeIdentifier linux-x64 -SkipMoveLibraries:$SkipMoveLibraries
#Build-Project -Configuration $Configuration -Game YR -Engine UniversalGL -TargetFramework net6.0 -RuntimeIdentifier win10-x64 -SkipMoveLibraries:$SkipMoveLibraries
#Build-Project -Configuration $Configuration -Game YR -Engine UniversalGL -TargetFramework net6.0 -RuntimeIdentifier osx.12-x64 -SkipMoveLibraries:$SkipMoveLibraries
#Build-Project -Configuration $Configuration -Game YR -Engine UniversalGL -TargetFramework net6.0 -RuntimeIdentifier ubuntu.22.04-x64 -SkipMoveLibraries:$SkipMoveLibraries
#Build-Project -Configuration $Configuration -Game YR -Engine UniversalGL -TargetFramework net6.0 -RuntimeIdentifier ios.15-arm64 -SkipMoveLibraries:$SkipMoveLibraries
#Build-Project -Configuration $Configuration -Game YR -Engine UniversalGL -TargetFramework net6.0 -RuntimeIdentifier android-arm64 -SkipMoveLibraries:$SkipMoveLibraries
#Build-Project -Configuration $Configuration -Game YR -Engine UniversalGL -TargetFramework net6.0 -RuntimeIdentifier alpine.3.9-arm -SkipMoveLibraries:$SkipMoveLibraries
#Build-Project -Configuration $Configuration -Game YR -Engine UniversalGL -TargetFramework net6.0 -RuntimeIdentifier tizen.4.0.0-arm64 -SkipMoveLibraries:$SkipMoveLibraries
function Build-Ares {
  [CmdletBinding()]
  param (
    [Parameter(Mandatory)]
    [Configurations]
    $Configuration,
    [Parameter()]
    [Switch]
    $SkipMoveLibraries
  )

  process {
    Clear-Compiled (Join-Path $Script:ClientCompiledTarget "Ares")

    Build-Project -Configuration $Configuration -Game Ares -Engine UniversalGL -TargetFramework net6.0 -RuntimeIdentifier any -SkipMoveLibraries:$SkipMoveLibraries

    If ($IsWindows)
    {
      Build-Project -Configuration $Configuration -Game Ares -Engine WindowsDX -TargetFramework net6.0-windows10.0.22000.0 -SkipMoveLibraries:$SkipMoveLibraries
      Build-Project -Configuration $Configuration -Game Ares -Engine WindowsGL -TargetFramework net6.0-windows10.0.22000.0 -SkipMoveLibraries:$SkipMoveLibraries
      Build-Project -Configuration $Configuration -Game Ares -Engine WindowsXNA -TargetFramework net6.0-windows10.0.22000.0 -PlatformTarget x86 -SkipMoveLibraries:$SkipMoveLibraries

      Build-Project -Configuration $Configuration -Game Ares -Engine WindowsDX -TargetFramework net48 -SkipMoveLibraries:$SkipMoveLibraries
      Build-Project -Configuration $Configuration -Game Ares -Engine WindowsGL -TargetFramework net48 -SkipMoveLibraries:$SkipMoveLibraries
      Build-Project -Configuration $Configuration -Game Ares -Engine WindowsXNA -TargetFramework net48 -PlatformTarget x86 -SkipMoveLibraries:$SkipMoveLibraries
    }
  }
}

function Build-TS {
  [CmdletBinding()]
  param (
    [Parameter(Mandatory)]
    [Configurations]
    $Configuration,
    [Parameter()]
    [Switch]
    $SkipMoveLibraries
  )

  process {
    Clear-Compiled (Join-Path $Script:ClientCompiledTarget "TS")

    Build-Project -Configuration $Configuration -Game TS -Engine UniversalGL -TargetFramework net6.0 -RuntimeIdentifier any -SkipMoveLibraries:$SkipMoveLibraries

    If ($IsWindows)
    {
      Build-Project -Configuration $Configuration -Game TS -Engine WindowsDX -TargetFramework net6.0-windows10.0.22000.0 -SkipMoveLibraries:$SkipMoveLibraries
      Build-Project -Configuration $Configuration -Game TS -Engine WindowsGL -TargetFramework net6.0-windows10.0.22000.0 -SkipMoveLibraries:$SkipMoveLibraries
      Build-Project -Configuration $Configuration -Game TS -Engine WindowsXNA -TargetFramework net6.0-windows10.0.22000.0 -PlatformTarget x86 -SkipMoveLibraries:$SkipMoveLibraries

      Build-Project -Configuration $Configuration -Game TS -Engine WindowsDX -TargetFramework net48 -SkipMoveLibraries:$SkipMoveLibraries
      Build-Project -Configuration $Configuration -Game TS -Engine WindowsGL -TargetFramework net48 -SkipMoveLibraries:$SkipMoveLibraries
      Build-Project -Configuration $Configuration -Game TS -Engine WindowsXNA -TargetFramework net48 -PlatformTarget x86 -SkipMoveLibraries:$SkipMoveLibraries
    }
  }
}

function Build-YR {
  [CmdletBinding()]
  param (
    [Parameter(Mandatory)]
    [Configurations]
    $Configuration,
    [Parameter()]
    [Switch]
    $SkipMoveLibraries
  )

  process {
    Clear-Compiled (Join-Path $Script:ClientCompiledTarget "YR")

    Build-Project -Configuration $Configuration -Game YR -Engine UniversalGL -TargetFramework net6.0 -RuntimeIdentifier any -SkipMoveLibraries:$SkipMoveLibraries

    If ($IsWindows)
    {
      Build-Project -Configuration $Configuration -Game YR -Engine WindowsDX -TargetFramework net6.0-windows10.0.22000.0 -SkipMoveLibraries:$SkipMoveLibraries
      Build-Project -Configuration $Configuration -Game YR -Engine WindowsGL -TargetFramework net6.0-windows10.0.22000.0 -SkipMoveLibraries:$SkipMoveLibraries
      Build-Project -Configuration $Configuration -Game YR -Engine WindowsXNA -TargetFramework net6.0-windows10.0.22000.0 -PlatformTarget x86 -SkipMoveLibraries:$SkipMoveLibraries

      Build-Project -Configuration $Configuration -Game YR -Engine WindowsDX -TargetFramework net48 -SkipMoveLibraries:$SkipMoveLibraries
      Build-Project -Configuration $Configuration -Game YR -Engine WindowsGL -TargetFramework net48 -SkipMoveLibraries:$SkipMoveLibraries
      Build-Project -Configuration $Configuration -Game YR -Engine WindowsXNA -TargetFramework net48 -PlatformTarget x86 -SkipMoveLibraries:$SkipMoveLibraries
    }
  }
}

function Build-All {
  [CmdletBinding()]
  param (
    [Parameter(Mandatory)]
    [Configurations]
    $Configuration,
    [Parameter()]
    [Switch]
    $SkipMoveLibraries
  )

  process {
    Build-Ares $Configuration -SkipMoveLibraries:$SkipMoveLibraries
    Build-TS $Configuration -SkipMoveLibraries:$SkipMoveLibraries
    Build-YR $Configuration -SkipMoveLibraries:$SkipMoveLibraries
  }
}