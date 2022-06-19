#!/usr/bin/env pwsh
#Requires -Version 5.0

using namespace System.IO


function Get-FileNameWithoutExtension {
  [CmdletBinding()]
  param (
    # FileInfo
    [Parameter(Mandatory)]
    [FileInfo]
    $File
  )

  process {
    return $File.Name.Substring(0, $File.Name.IndexOf('.'))
  }
}

function Get-FileExtension {
  [CmdletBinding()]
  param (
    # FileInfo
    [Parameter(Mandatory)]
    [FileInfo]
    $File
  )

  process {
    return $File.Name.Substring($File.Name.IndexOf('.'))
  }
}