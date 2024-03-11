#!/usr/bin/env pwsh
#Requires -Version 7.2

# /// WARNING /// WARNING /// WARNING ///
#
# DO NOT CHANGE OUTPUT FOR THIS SCRIPT! 
#
# /// WARNING /// WARNING /// WARNING ///

# 这个脚本可以通过计算通过 `.\build.ps1 -NoMove` 命令生成的内容生成通用程序集列表。

[CmdletBinding()]
param (
  [Parameter()]
  [string]
  $Game = 'Ares',
  [Parameter()]
  [switch]
  $Net8
)

[string]$Script:RepoRoot = Split-Path $PSScriptRoot
[string]$Script:CompiledRoot = Join-Path $RepoRoot 'Compiled'
[string]$Script:GamePath = Join-Path $CompiledRoot $Game
[string]$Script:Resources = Join-Path $GamePath 'Resources'
[string]$Script:Binaries = Join-Path $Resources 'Binaries'
if ($Net8) {
  $Script:Binaries = Join-Path $Resources 'BinariesNET8'
}

[System.Collections.Generic.List[string]]$Script:Engines = @('OpenGL', 'Windows', 'XNA')
if ($Net8) {
  $Script:Engines.Add('UniversalGL')
}

[hashtable]$Script:FileHashTable = @{}
$Script:Engines | ForEach-Object {
  [string]$Private:Engine = $PSItem
  [string]$Private:PlatformFolder = Join-Path $Binaries $Private:Engine

  Get-ChildItem $Private:PlatformFolder | Where-Object {
    $PSItem -is [System.IO.FileInfo]
  } | ForEach-Object {
    if (!$Script:FileHashTable.ContainsKey($PSItem.Name)) {
      $Script:FileHashTable[$PSItem.Name] = [hashtable]@{}
    }

    $Script:FileHashTable[$PSItem.Name][$Engine] = Get-FileHash $PSItem
  }
}

$Script:FileList = $Script:FileHashTable.Keys | Where-Object {
  $Private:Key = $PSItem
  if ($Script:FileHashTable[$Private:Key].Count -ne $Script:Engines.Count) {
    return $false
  }
  [string]$hash = $null
  foreach ($item in $Script:FileHashTable[$Private:Key].Values) {
    if ([string]::IsNullOrEmpty($hash)) {
      $hash = $item.Hash
    }
    elseif ($hash -ne $item.Hash) {
      return $false
    }
  }
  return $true
}

$Script:FileList | Sort-Object -Unique