#!/usr/bin/env pwsh
#Requires -Version 7.2

param($Configuration = "Release", $AssemblySemVer = "0.0.0", $AssemblySemFileVer = "0.0.0.0", $InformationalVersion = "0.0.0-local")

. $PSScriptRoot\Common.ps1

@('net8.0', 'net48') | ForEach-Object {
  . "$PSScriptRoot\Build-All-Framework.ps1" $Configuration $_ $AssemblySemVer $AssemblySemFileVer $InformationalVersion
}