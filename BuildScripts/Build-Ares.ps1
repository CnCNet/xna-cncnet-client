#!/usr/bin/env pwsh
#Requires -Version 7.2

[CmdletBinding()]
param (
  [Parameter()]
  [string]
  $Configuration = 'Release',
  [Parameter()]
  [Switch]
  [bool]
  $SkipMoveLibraries
)

# Imports
. (Join-Path $PSScriptRoot "lib" "Enums.ps1")
. (Join-Path $PSScriptRoot "lib" "BuildTools.ps1")

Build-Ares $Configuration -SkipMoveLibraries:$SkipMoveLibraries