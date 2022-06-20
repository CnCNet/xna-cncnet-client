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

  begin {
    $Private:ClientCommonLibraries = Get-Content $PSScriptRoot\CommonFileList.txt
  }

  process {
    Get-ChildItem $Path | ForEach-Object {
      if ($Private:ClientCommonLibraries.Contains($_.Name)) {
        $Private:TargetPath = Join-Path $Path .. $_.Name
        if (!(Test-Path $Private:TargetPath)) {
          Move-Item -Path $_ -Destination $Private:TargetPath -Force
        }else{
          Remove-Item -Path $_ -Force
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
    $Target
  )

  process {
    # $Private:TargetPath = Join-Path $Target "client$ClientSuffix$(Get-FileExtension $File)"
    if (Test-Path $File.FullName) {
      Move-Item $File.FullName $Target -Force
    }
    else {
      Write-Error "File not found: $File"
    }
  }
}