#!/usr/bin/env pwsh
#Requires -Version 7.2

param($Configuration = "Release")

. $PSScriptRoot\Common.ps1

@('Ares', 'TS', 'YR') | ForEach-Object {
  . "$PSScriptRoot\Build-$_-net6.0.ps1" $Configuration
}