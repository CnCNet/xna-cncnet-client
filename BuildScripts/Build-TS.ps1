#!/usr/bin/env pwsh
#Requires -Version 7.2

param($Configuration = "Release")

. $PSScriptRoot\Common.ps1

$Game = "TS"

Build-Project $Configuration $Game UniversalGL net8.0
if ($IsWindows) {
  @('WindowsDX', 'WindowsGL', 'WindowsXNA') | ForEach-Object {
    $Engine = $_
    @('net48', 'net8.0-windows') | ForEach-Object {
      Build-Project $Configuration $Game $Engine $_
    }
  }
}