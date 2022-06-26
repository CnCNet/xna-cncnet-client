#!/usr/bin/env pwsh
#Requires -Version 5.0

# Imports
. $PSScriptRoot\Constants.ps1
. $PSScriptRoot\Enums.ps1
. $PSScriptRoot\BuildCore.ps1
. $PSScriptRoot\ClearTools.ps1

# See https://docs.microsoft.com/en-us/dotnet/core/rid-catalog for adding specific RuntimeIdentifiers
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
    Clear-Compiled $Script:ClientCompiledTarget\Ares

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
    Clear-Compiled $Script:ClientCompiledTarget\TS

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
    Clear-Compiled $Script:ClientCompiledTarget\YR
       
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