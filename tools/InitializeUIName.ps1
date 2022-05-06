#!/usr/bin/env pwsh
#Requires -Version 5.0

using namespace System.IO

[CmdletBinding()]
param (
  [Parameter(Mandatory)]
  [string]
  $InputFile,
  # OutputFile
  [Parameter()]
  [string]
  $OutputFile
)
if (!$OutputFile) {
  $OutputFile = [Path]::GetFileNameWithoutExtension($InputFile)
  $OutputFile += '.out'
  $OutputFile += '.ini'
}

Remove-Item $OutputFile -Force

$data = Get-Content $InputFile
for ($i = 0; $i -lt $data.Length; $i++) {
  $line = $data[$i]
  Write-Progress 'Appending "UIName" ...' -PercentComplete ($i / $data.Length * 100)
  [string]$uiname = [string]::Empty
  if ($line.StartsWith('[')) {
    # Section
    $current = $line.Substring(1, $line.IndexOf(']') - 1)
    if ($current.Contains('Maps') -and (`
          $current.Contains('\') -or `
          $current.Contains('/'))) {
      $uiname = [Path]::GetFileNameWithoutExtension($current)
    }
  }

  $line | Out-File $OutputFile -Append
  if (![string]::IsNullOrEmpty($uiname)) {
    "UIName=$uiname" | Out-File $OutputFile -Append
  }
}