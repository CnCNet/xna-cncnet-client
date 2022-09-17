#!/usr/bin/env pwsh
#Requires -Version 7.2

Param([Parameter(Mandatory=$false)] [string] $Configuration = "Release")

$path = Split-Path $MyInvocation.MyCommand.Path -Parent

dotnet publish $path\..\DXMainClient\DXMainClient.csproj -c $Configuration -p:GAME=TS -p:ENGINE=WindowsXNA -f net48 -a x86 -o ..\Compiled\TS\net48\Resources\Binaries\XNA
if ($LASTEXITCODE) { throw }
dotnet publish $path\..\DXMainClient\DXMainClient.csproj -c $Configuration -p:GAME=TS -p:ENGINE=WindowsDX -f net48 -o ..\Compiled\TS\net48\Resources\Binaries\Windows
if ($LASTEXITCODE) { throw }
dotnet publish $path\..\DXMainClient\DXMainClient.csproj -c $Configuration -p:GAME=TS -p:ENGINE=WindowsGL -f net48 -o ..\Compiled\TS\net48\Resources\Binaries\OpenGL
if ($LASTEXITCODE) { throw }