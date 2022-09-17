#!/usr/bin/env pwsh
#Requires -Version 7.2

Param([Parameter(Mandatory=$false)] [string] $Configuration = "Release")

$path = Split-Path $MyInvocation.MyCommand.Path -Parent

& $path\Build-Ares-net6.0.ps1 $Configuration

If ($IsWindows)
{
    & $path\Build-Ares-net48.ps1 $Configuration
}