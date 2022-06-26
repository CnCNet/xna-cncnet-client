#!/usr/bin/env pwsh
#Requires -Version 5.0

. $PSScriptRoot\Enums.ps1

$Script:SuffixMap = @{
  [Engines]::WindowsDX  = "dx"
  [Engines]::WindowsGL  = "ogl"
  [Engines]::UniversalGL  = "ogl"
  [Engines]::WindowsXNA = "xna"
}

$Script:PlatformNameMap = @{
  [Engines]::WindowsDX  = "Windows"
  [Engines]::WindowsGL  = "OpenGL"
  [Engines]::UniversalGL  = "OpenGL"
  [Engines]::WindowsXNA = "XNA"
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