#!/usr/bin/env pwsh
#Requires -Version 7.2

param($Configuration = "Release")

. $PSScriptRoot\Common.ps1

@('WindowsDX', 'WindowsGL', 'WindowsXNA') | ForEach-Object {
  Build-Project $Configuration YR $_ net48
}