#!/usr/bin/env pwsh
#Requires -Version 5.0

$Script:ClientProjectPath = Resolve-Path (Join-Path $PSScriptRoot .. .. DXMainClient)
$Script:ClientCompiledTarget = Resolve-Path (Join-Path $PSScriptRoot .. .. Compiled)
$Script:ClientCommonLibraries = @(
  'ClientUpdater'
  'DiscordRPC'
  'Localization'
  'lzo.net'
  'Microsoft.CodeAnalysis.CSharp'
  'Microsoft.CodeAnalysis'
  'Microsoft.CodeAnalysis.VisualBasic'
  'Microsoft.Windows.SDK.NET'
  'Newtonsoft.Json'
  'OpenMcdf'
  'Rampastring.Tools'
  'System.Management'
  'WinRT.Runtime'
)
