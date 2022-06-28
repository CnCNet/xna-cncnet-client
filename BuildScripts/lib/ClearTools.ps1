#!/usr/bin/env pwsh
#Requires -Version 7.2

function Clear-Compiled {
  [CmdletBinding()]
  param (
    [Parameter(Mandatory)]
    [string]
    $Path
  )

  process {
    if (Test-Path $Path) {
      Remove-Item $Path -Recurse -Force
    }
  }
}