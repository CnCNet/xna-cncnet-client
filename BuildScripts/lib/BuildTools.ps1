#!/usr/bin/env pwsh
#Requires -Version 5.0

# Imports
. $PSScriptRoot\Constants.ps1
. $PSScriptRoot\Enums.ps1
. $PSScriptRoot\BuildCore.ps1
. $PSScriptRoot\ClearTools.ps1

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

    Build-Project -Configuration $Configuration -Game Ares -Engine DX -TargetFramework net6.0-windows10.0.22000.0 -SkipMoveLibraries:$SkipMoveLibraries
    Build-Project -Configuration $Configuration -Game Ares -Engine GL -TargetFramework net6.0-windows10.0.22000.0 -SkipMoveLibraries:$SkipMoveLibraries
    Build-Project -Configuration $Configuration -Game Ares -Engine XNA -TargetFramework net6.0-windows10.0.22000.0 -SkipMoveLibraries:$SkipMoveLibraries

    Build-Project -Configuration $Configuration -Game Ares -Engine DX -TargetFramework net48 -SkipMoveLibraries:$SkipMoveLibraries
    Build-Project -Configuration $Configuration -Game Ares -Engine GL -TargetFramework net48 -SkipMoveLibraries:$SkipMoveLibraries
    Build-Project -Configuration $Configuration -Game Ares -Engine XNA -TargetFramework net48 -SkipMoveLibraries:$SkipMoveLibraries
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

    Build-Project -Configuration $Configuration -Game TS -Engine DX -TargetFramework net6.0-windows10.0.22000.0 -SkipMoveLibraries:$SkipMoveLibraries
    Build-Project -Configuration $Configuration -Game TS -Engine GL -TargetFramework net6.0-windows10.0.22000.0 -SkipMoveLibraries:$SkipMoveLibraries
    Build-Project -Configuration $Configuration -Game TS -Engine XNA -TargetFramework net6.0-windows10.0.22000.0 -SkipMoveLibraries:$SkipMoveLibraries

    Build-Project -Configuration $Configuration -Game TS -Engine DX -TargetFramework net48 -SkipMoveLibraries:$SkipMoveLibraries
    Build-Project -Configuration $Configuration -Game TS -Engine GL -TargetFramework net48 -SkipMoveLibraries:$SkipMoveLibraries
    Build-Project -Configuration $Configuration -Game TS -Engine XNA -TargetFramework net48 -SkipMoveLibraries:$SkipMoveLibraries
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

    Build-Project -Configuration $Configuration -Game YR -Engine DX -TargetFramework net6.0-windows10.0.22000.0 -SkipMoveLibraries:$SkipMoveLibraries
    Build-Project -Configuration $Configuration -Game YR -Engine GL -TargetFramework net6.0-windows10.0.22000.0 -SkipMoveLibraries:$SkipMoveLibraries
    Build-Project -Configuration $Configuration -Game YR -Engine XNA -TargetFramework net6.0-windows10.0.22000.0 -SkipMoveLibraries:$SkipMoveLibraries

    Build-Project -Configuration $Configuration -Game YR -Engine DX -TargetFramework net48 -SkipMoveLibraries:$SkipMoveLibraries
    Build-Project -Configuration $Configuration -Game YR -Engine GL -TargetFramework net48 -SkipMoveLibraries:$SkipMoveLibraries
    Build-Project -Configuration $Configuration -Game YR -Engine XNA -TargetFramework net48 -SkipMoveLibraries:$SkipMoveLibraries
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