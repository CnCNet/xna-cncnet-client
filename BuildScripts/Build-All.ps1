#!/usr/bin/env pwsh
#Requires -Version 7.2

Param([Parameter(Mandatory=$false)] [string] $Configuration = "Release")

.\Build-All-net6.0.ps1 $Configuration
.\Build-All-net48.ps1 $Configuration