#!/usr/bin/env pwsh
#Requires -Version 7.2

param($Configuration = "Release")

. $PSScriptRoot\Common.ps1

. $PSScriptRoot\Build-All-net6.0.ps1 $Configuration
. $PSScriptRoot\Build-All-net48.ps1 $Configuration