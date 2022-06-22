#!/usr/bin/env pwsh
#Requires -Version 5.0

using namespace System.IO

. $PSScriptRoot\Enums.ps1
. $PSScriptRoot\FileTools.ps1

function Move-Libraries {
  [CmdletBinding()]
  param (
    [Parameter(Mandatory)]
    [string]
    $Path,
    [Parameter(Mandatory)]
    [string]
    $Destination,
    [Parameter()]
    [Switch]
    $Special,
    [Parameter()]
    [Engines]
    $Engine
  )

  begin {
    $Private:ClientSpecialLibraries = $null
    if ($Special) {
      if ($null -eq $Engine) {
        Get-Help Move-Libraries
        throw 'Must have Engine when use -Special'
      }

      $Private:ClientSpecialLibraries = Get-Content $PSScriptRoot\Special$($Engine)List.txt
    }


    Write-Debug ""
    Write-Debug "Invoke Move-Libraries"
    Write-Debug ""
    Write-Debug "Special: $Special; Engine: $Engine"
    Write-Debug "Path: $Path"
    Write-Debug "Destination: $Destination"
    Write-Debug "ClientSpecialLibraries: $Private:ClientSpecialLibraries"
  }

  process {
    $tmp = Get-ChildItem $Path

    if ($Special) {
      $tmp = $tmp | Where-Object Name -In $Private:ClientSpecialLibraries
    }

    $tmp | ForEach-Object {
      $Private:TargetPath = Join-Path $Destination $_.Name
      if (!(Test-Path $Private:TargetPath)) {
        # If File Not Exists
        Write-Debug "Move $_ to $Private:TargetPath"
        Move-Item -Path $_ -Destination $Private:TargetPath -Force
      }
      else {
        Write-Debug "Delete $_"
        Remove-Item -Path $_ -Recurse -Force
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
    if (Test-Path $File.FullName) {
      Move-Item $File.FullName $Target -Force
    }
    else {
      Write-Error "File not found: $File"
    }
  }
}