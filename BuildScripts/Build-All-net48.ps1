#!/usr/bin/env pwsh
#Requires -Version 7.2

Param([Parameter(Mandatory=$false)] [string] $Configuration = "Release")

.\Build-Ares-net48.ps1 $Configuration
.\Build-TS-net48.ps1 $Configuration
.\Build-YR-net48.ps1 $Configuration