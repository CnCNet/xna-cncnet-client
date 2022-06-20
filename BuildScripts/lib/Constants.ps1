#!/usr/bin/env pwsh
#Requires -Version 5.0

$Private:tmp = Join-Path $PSScriptRoot .. .. Compiled
if (!(Test-Path $Private:tmp)){
  New-Item -ItemType Directory -Force -Path $Private:tmp
}

$Script:ClientCompiledTarget = Resolve-Path $Private:tmp
$Script:ClientProjectPath = Resolve-Path (Join-Path $PSScriptRoot .. .. DXMainClient)
