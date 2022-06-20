#!/usr/bin/env pwsh
#Requires -Version 5.0

[CmdletBinding()]
param (
  [Parameter()]
  [string]
  $Configuration = 'Release',
  [Parameter()]
  [Switch]
  [bool]
  $SkipMoveCommonLibraries
)

# Imports
. $PSScriptRoot\lib\Enums.ps1
. $PSScriptRoot\lib\BuildTools.ps1

Build-YR $Configuration -SkipMoveCommonLibraries:$SkipMoveCommonLibraries
