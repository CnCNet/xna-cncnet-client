#!/usr/bin/env pwsh
#Requires -Version 7.2

[CmdletBinding()]
param (
  [Parameter(Mandatory)]
  [string]
  $Path
)

New-Item -Path (Join-Path $PSScriptRoot "CommonLibList.txt") -ItemType File -Force

Get-ChildItem (Join-Path $Path "Windows") | ForEach-Object {
  $Private:DXFile = (Join-Path $Path "Windows" $($_.Name))
  $Private:GLFile = (Join-Path $Path "OpenGL" $($_.Name))
  $Private:XNAFile = (Join-Path $Path "XNA" $($_.Name))
  if (
    ($_.Name -ne 'runtimes') `
      -and (Test-Path $Private:GLFile) `
      -and (Test-Path $Private:XNAFile)
  ) {
    $Private:DXHash = (Get-FileHash $Private:DXFile).Hash
    $Private:GLHash = (Get-FileHash $Private:GLFile).Hash
    $Private:XNAHash = (Get-FileHash $Private:XNAFile).Hash
    if (
      ($Private:DXHash -eq $Private:GLHash) `
        -and ($Private:DXHash -eq $Private:XNAHash)
    ) {
      $_.Name | Out-File (Join-Path $PSScriptRoot "CommonLibList.txt") -Append -Encoding utf8
    }
  }
}