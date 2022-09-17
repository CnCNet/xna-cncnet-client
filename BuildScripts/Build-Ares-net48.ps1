#!/usr/bin/env pwsh
#Requires -Version 7.2

Param([Parameter(Mandatory=$false)] [string] $Configuration = "Release")

dotnet publish ..\DXMainClient\DXMainClient.csproj -c $Configuration -p:GAME=Ares -p:ENGINE=WindowsXNA -f net48 -a x86 -o ..\Compiled\Ares\net48\Resources\Binaries\XNA
if ($LASTEXITCODE) { throw }
dotnet publish ..\DXMainClient\DXMainClient.csproj -c $Configuration -p:GAME=Ares -p:ENGINE=WindowsDX -f net48 -o ..\Compiled\Ares\net48\Resources\Binaries\Windows
if ($LASTEXITCODE) { throw }
dotnet publish ..\DXMainClient\DXMainClient.csproj -c $Configuration -p:GAME=Ares -p:ENGINE=WindowsGL -f net48 -o ..\Compiled\Ares\net48\Resources\Binaries\OpenGL
if ($LASTEXITCODE) { throw }