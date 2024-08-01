#!/usr/bin/env pwsh
#Requires -Version 7.2

#####################################################################
#
# Note: 
#    Be careful to synchronize changes to `Directory.Build.targets`
#    when making changes to paths.
#
#####################################################################

<#
.SYNOPSIS
  Builds XNA CnCNet Client using specified parameters.
.DESCRIPTION
  You can use this script to make publish packages for your game.
.PARAMETER Games
  What game(s) to build the client for?
  If not specified - builds the client for all games.
.PARAMETER IsDebug
  Build projects in debug mode.
.PARAMETER Log
  Detail log.
.PARAMETER NoClean
  Do not clean Compiled folder.
.PARAMETER NoMove
  Do not make folder structure.
.EXAMPLE
  build.ps1
  Build for all games.
.EXAMPLE
  build.ps1 Ares
  Build for ares game.
.EXAMPLE
  build.ps1 Ares -IsDebug
  Build for ares game on debug mode.
#>
param(
  [Parameter(ValueFromRemainingArguments)]
  [string[]]
  $Games,
  [Parameter()]
  [switch]
  $IsDebug,
  [Parameter()]
  [switch]
  $Log,
  [Parameter()]
  [switch]
  $NoClean,
  [Parameter()]
  [switch]
  $NoMove
)

$Script:ConfigurationSuffix = 'Release'
if ($IsDebug) {
  $Script:ConfigurationSuffix = 'Debug'
}

$Script:RepoRoot = Split-Path $PSScriptRoot
$Script:ProjectPath = Join-Path $RepoRoot 'DXMainClient' 'DXMainClient.csproj'
$Script:CompiledRoot = Join-Path $RepoRoot 'Compiled'
$Script:EngineSubFolderMap = @{
  'UniversalGL' = 'UniversalGL'
  'WindowsDX'   = 'Windows'
  'WindowsGL'   = 'OpenGL'
  'WindowsXNA'  = 'XNA'
}
$Script:FrameworkBinariesFolderMap = @{
  'net48'          = 'Binaries'
  'net8.0'         = 'BinariesNET8'
  'net8.0-windows' = 'BinariesNET8'
}

if (!$NoClean -AND (Test-Path $Script:CompiledRoot)) {
  Remove-Item -Recurse -Force -LiteralPath $Script:CompiledRoot
}

if ($null -EQ $IsWindows -AND 'Desktop' -EQ $PSEdition) {
  $Script:IsWindows = $true
}

function Script:Invoke-BuildProject {
  [CmdletBinding(DefaultParameterSetName = 'ByGame')]
  param (
    [Parameter(ParameterSetName = 'ByGame', Position = 0)]
    [Parameter(Mandatory, ParameterSetName = 'Detail', Position = 0)]
    [string]
    $Game,
    [Parameter(Mandatory, ParameterSetName = 'Detail')]
    [string]
    $Engine,
    [Parameter(Mandatory, ParameterSetName = 'Detail')]
    [string]
    $Framework
  )
  
  process {
    if ($Engine) {
      $Output = Join-Path $CompiledRoot $Game $Output 'Resources' ($FrameworkBinariesFolderMap[$Framework]) ($EngineSubFolderMap[$Engine])
  
      $Private:ArgumentList = [System.Collections.Generic.List[string]]::new(11)
      $Private:ArgumentList.Add('publish')
      $Private:ArgumentList.Add("$ProjectPath")
      $Private:ArgumentList.Add('--graph')
      $Private:ArgumentList.Add("--configuration:${Game}${Engine}$Script:ConfigurationSuffix")
      $Private:ArgumentList.Add("--framework:$Framework")
      $Private:ArgumentList.Add("--output:$Output")
      $Private:ArgumentList.Add('-property:SatelliteResourceLanguages=en')
      if ($Log) {
        $Private:ArgumentList.Add('-verbosity:diagnostic')
      }
      if ($NoMove) {
        $Private:ArgumentList.Add('-property:NoMove=true')
      }
      # $Private:ArgumentList.Add("-property:AssemblyVersion=$AssemblySemVer")
      # $Private:ArgumentList.Add("-property:FileVersion=$AssemblySemFileVer")
      # $Private:ArgumentList.Add("-property:InformationalVersion=$InformationalVersion")
  
      if ($Engine -eq 'WindowsXNA') {
        $Private:ArgumentList.Add('--arch=x86')
      }
  
      & 'dotnet' $Private:ArgumentList  
      if ($LASTEXITCODE) {
        throw "Build failed for ${Game}${Engine}$Script:ConfigurationSuffix $Framework"
      }
    }
    elseif ($Game) {
      Invoke-BuildProject -Game $Game -Engine 'UniversalGL' -Framework 'net8.0'
      if ($IsWindows) {
        @('WindowsDX', 'WindowsGL', 'WindowsXNA') | ForEach-Object {
          $Private:Engine = $PSItem
  
          @('net48', 'net8.0-windows') | ForEach-Object {
            $Private:Framework = $PSItem
  
            Invoke-BuildProject -Game $Game -Engine $Private:Engine -Framework $Private:Framework
          }
        }
      }
    }
    else {
      @('Ares', 'TS', 'YR') | ForEach-Object {
        Invoke-BuildProject -Game $PSItem
      }
    }
  }
}

if ($Games.Count -EQ 0) {
  Script:Invoke-BuildProject
}
else {
  $Games | ForEach-Object { Script:Invoke-BuildProject $PSItem }
}