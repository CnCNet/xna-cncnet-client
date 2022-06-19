#!/usr/bin/env pwsh
#Requires -Version 5.0

. $PSScriptRoot\Enums.ps1

$Script:SuffixMap = @{
  [Engines]::DX  = "dx"
  [Engines]::GL  = "ogl"
  [Engines]::XNA = "xna"
}

$Script:PlatformNameMap = @{
  [Engines]::DX  = "Windows"
  [Engines]::GL  = "OpenGL"
  [Engines]::XNA = "XNA"
}

function Get-Suffix {
  [CmdletBinding()]
  param (
    [Parameter(Mandatory)]
    [Engines]
    $Engine
  )
  process {
    return $Script:SuffixMap[$Engine]
  }
}

function Get-PlatformName {
  [CmdletBinding()]
  param (
    [Parameter(Mandatory)]
    [Engines]
    $Engine
  )
  process {
    return $Script:PlatformNameMap[$Engine]
  }
}