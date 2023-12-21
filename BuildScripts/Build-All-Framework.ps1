#!/usr/bin/env pwsh
#Requires -Version 7.2

param($Configuration = "Release", $Framework = "net8.0", $AssemblySemVer = "0.0.0", $AssemblySemFileVer = "0.0.0.0", $InformationalVersion = "0.0.0-local")

. $PSScriptRoot\Common.ps1

@('Ares', 'TS', 'YR') | ForEach-Object {
  . "$PSScriptRoot\Build-$_.ps1" $Configuration $Framework $AssemblySemVer $AssemblySemFileVer $InformationalVersion
}