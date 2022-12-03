#!/usr/bin/env pwsh
#Requires -Version 7.2

param($Configuration = "Release")

. $PSScriptRoot\Common.ps1

Build-Project $Configuration TS UniversalGL net7.0
if ($IsWindows) {
  @('WindowsDX', 'WindowsGL', 'WindowsXNA') | ForEach-Object {
    Build-Project $Configuration TS $_ net7.0-windows
  }
}