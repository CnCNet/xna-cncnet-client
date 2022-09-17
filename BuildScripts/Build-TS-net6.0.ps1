#!/usr/bin/env pwsh
#Requires -Version 7.2

Param([Parameter(Mandatory=$false)] [string] $Configuration = "Release")

dotnet publish ..\DXMainClient\DXMainClient.csproj -c $Configuration -p:GAME=TS -p:ENGINE=UniversalGL -f net6.0 -o ..\Compiled\TS\net6.0\any\Resources\Binaries\OpenGL
if ($LASTEXITCODE) { throw }

If ($IsWindows)
{
    dotnet publish ..\DXMainClient\DXMainClient.csproj -c $Configuration -p:GAME=TS -p:ENGINE=WindowsXNA -f net6.0-windows10.0.22000.0 -a x86 -o ..\Compiled\TS\net6.0-windows10.0.22000.0\Resources\Binaries\XNA
    if ($LASTEXITCODE) { throw }
    dotnet publish ..\DXMainClient\DXMainClient.csproj -c $Configuration -p:GAME=TS -p:ENGINE=WindowsDX -f net6.0-windows10.0.22000.0 -o ..\Compiled\TS\net6.0-windows10.0.22000.0\Resources\Binaries\Windows
    if ($LASTEXITCODE) { throw }
    dotnet publish ..\DXMainClient\DXMainClient.csproj -c $Configuration -p:GAME=TS -p:ENGINE=WindowsGL -f net6.0-windows10.0.22000.0 -o ..\Compiled\TS\net6.0-windows10.0.22000.0\Resources\Binaries\OpenGL
    if ($LASTEXITCODE) { throw }
}