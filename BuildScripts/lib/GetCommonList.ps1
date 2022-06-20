#!/usr/bin/env pwsh
#Requires -Version 5.0

[CmdletBinding()]
param (
  [Parameter(Mandatory)]
  [string]
  $Path
)

New-Item -Path $PSScriptRoot\CommonLibList.txt -ItemType File -Force

Get-ChildItem $Path\Windows\ | ForEach-Object {
  $Private:DXFile = "$Path\Windows\$($_.Name)"
  $Private:GLFile = "$Path\OpenGL\$($_.Name)"
  $Private:XNAFile = "$Path\XNA\$($_.Name)"
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
      $_.Name | Out-File $PSScriptRoot\CommonLibList.txt -Append -Encoding utf8
    }
  }
}