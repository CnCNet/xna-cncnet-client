#!/usr/bin/env pwsh
#Requires -Version 7.2

Param([Parameter(Mandatory=$false)] [string] $Configuration = "Release")

.\Build-Ares-net6.0.ps1 $Configuration

If ($IsWindows)
{
    .\Build-Ares-net48.ps1 $Configuration
}