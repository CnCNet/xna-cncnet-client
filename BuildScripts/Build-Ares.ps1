#!/usr/bin/env pwsh
#Requires -Version 7.2

param($Configuration = "Release")

. $PSScriptRoot\Common.ps1

. $PSScriptRoot\Build-Ares-net6.0.ps1 $Configuration

if ($IsWindows) {
  . $PSScriptRoot\Build-Ares-net48.ps1 $Configuration
}