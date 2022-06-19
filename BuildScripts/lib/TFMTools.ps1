#!/usr/bin/env pwsh
#Requires -Version 5.0

function Get-TargetFrameworkWithoutTFM {
  [CmdletBinding()]
  param (
    [Parameter(Mandatory)]
    [string]
    $TargetFramework
  )

  process {
    return ($TargetFramework -split '-')[0]
  }
}