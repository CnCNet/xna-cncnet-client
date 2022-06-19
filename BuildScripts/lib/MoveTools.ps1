#!/usr/bin/env pwsh
#Requires -Version 5.0

using namespace System.IO

. $PSScriptRoot\FileTools.ps1

function Move-CommonLibraries {
  [CmdletBinding()]
  param (
    [Parameter(Mandatory)]
    [string]
    $Path
  )

  process {
    Get-ChildItem $Path | ForEach-Object {
      if ($Script:ClientCommonLibraries.Contains([Path]::GetFileNameWithoutExtension($_))) {
        $Private:TargetPath = Join-Path $Path .. $_.Name
        if (!(Test-Path $Private:TargetPath)) {
          Move-Item -Path $_ -Destination $Private:TargetPath -Force
        }
        else {
          Write-Host "Skipping $_" -ForegroundColor Yellow
        }
      }
    }

  }

}

function Move-ClientBinaries {
  [CmdletBinding()]
  param (
    [Parameter(Mandatory)]
    [FileInfo]
    $File,
    [Parameter(Mandatory)]
    [string]
    $Target,
    [Parameter(Mandatory)]
    [string]
    $ClientSuffix
  )

  process {
    $Private:TargetPath = Join-Path $Target "client$ClientSuffix$(Get-FileExtension $File)"
    if (Test-Path $File.FullName) {
      Move-Item $File.FullName $Private:TargetPath -Force
    }
    else {
      Write-Error "File not found: $File"
    }
  }
}