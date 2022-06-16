@echo off
if [%1]==[] echo Missing game parameter (YR, TS or Ares) && exit /b

for /f "delims=" %%a in ('echo Creating %1 .NET4.8 Folders') do ( echo %%a & title %%a )
if exist ..\Compiled\%1\net48\ ( rd /s /q ..\Compiled\%1\net48 || echo ERROR && exit /b )
mkdir ..\Compiled\%1\net48\Resources\Binaries\Windows || echo ERROR && exit /b
mkdir ..\Compiled\%1\net48\Resources\Binaries\OpenGL\x64 || echo ERROR && exit /b
mkdir ..\Compiled\%1\net48\Resources\Binaries\OpenGL\x86 || echo ERROR && exit /b
mkdir ..\Compiled\%1\net48\Resources\Binaries\XNA || echo ERROR && exit /b

for /f "delims=" %%a in ('echo Publishing %1 .NET4.8 DirectX') do ( echo %%a & title %%a )
dotnet publish ..\DXMainClient\DXMainClient.csproj -c %1DXRelease -o ..\Compiled\%1\net48\DX -f net48 || echo ERROR && exit /b
for /f "delims=" %%a in ('echo Publishing %1 .NET4.8 OpenGL') do ( echo %%a & title %%a )
dotnet publish ..\DXMainClient\DXMainClient.csproj -c %1GLRelease -o ..\Compiled\%1\net48\GL -f net48 || echo ERROR && exit /b
for /f "delims=" %%a in ('echo Publishing %1 .NET4.8 XNA') do ( echo %%a & title %%a )
dotnet publish ..\DXMainClient\DXMainClient.csproj -c %1XNARelease -o ..\Compiled\%1\net48\XNA -f net48 -p:PlatformTarget=x86 || echo ERROR && exit /b

for /f "delims=" %%a in ('echo Structuring %1 .NET4.8 Main Files') do ( echo %%a & title %%a )
xcopy ..\Compiled\%1\net48\DX\*.* ..\Compiled\%1\net48\Resources\Binaries /e || echo ERROR && exit /b
rd /s /q ..\Compiled\%1\net48\DX || echo ERROR && exit /b

for /f "delims=" %%a in ('echo Structuring %1 .NET4.8 DirectX Files') do ( echo %%a & title %%a )
move ..\Compiled\%1\net48\Resources\Binaries\DXMainClient.* ..\Compiled\%1\net48\Resources || echo ERROR && exit /b
move ..\Compiled\%1\net48\Resources\Binaries\SharpDX.* ..\Compiled\%1\net48\Resources\Binaries\Windows || echo ERROR && exit /b
move ..\Compiled\%1\net48\Resources\Binaries\MonoGame.* ..\Compiled\%1\net48\Resources\Binaries\Windows || echo ERROR && exit /b
move ..\Compiled\%1\net48\Resources\Binaries\Rampastring.XNAUI.DX.* ..\Compiled\%1\net48\Resources\Binaries\Windows || echo ERROR && exit /b
move ..\Compiled\%1\net48\Resources\Binaries\ClientCore.* ..\Compiled\%1\net48\Resources\Binaries\Windows || echo ERROR && exit /b
move ..\Compiled\%1\net48\Resources\Binaries\ClientGUI.* ..\Compiled\%1\net48\Resources\Binaries\Windows || echo ERROR && exit /b
move ..\Compiled\%1\net48\Resources\Binaries\DTAConfig.* ..\Compiled\%1\net48\Resources\Binaries\Windows || echo ERROR && exit /b
ren ..\Compiled\%1\net48\Resources\DXMainClient.exe.config clientdx.exe.config || echo ERROR && exit /b
ren ..\Compiled\%1\net48\Resources\DXMainClient.exe clientdx.exe || echo ERROR && exit /b
ren ..\Compiled\%1\net48\Resources\DXMainClient.pdb clientdx.pdb || echo ERROR && exit /b

for /f "delims=" %%a in ('echo Structuring %1 .NET4.8 OpenGL Files') do ( echo %%a & title %%a )
move ..\Compiled\%1\net48\GL\DXMainClient.* ..\Compiled\%1\net48\Resources || echo ERROR && exit /b
move ..\Compiled\%1\net48\GL\MonoGame.* ..\Compiled\%1\net48\Resources\Binaries\OpenGL || echo ERROR && exit /b
move ..\Compiled\%1\net48\GL\Rampastring.XNAUI.GL.* ..\Compiled\%1\net48\Resources\Binaries\OpenGL || echo ERROR && exit /b
move ..\Compiled\%1\net48\GL\ClientCore.* ..\Compiled\%1\net48\Resources\Binaries\OpenGL || echo ERROR && exit /b
move ..\Compiled\%1\net48\GL\ClientGUI.* ..\Compiled\%1\net48\Resources\Binaries\OpenGL || echo ERROR && exit /b
move ..\Compiled\%1\net48\GL\DTAConfig.* ..\Compiled\%1\net48\Resources\Binaries\OpenGL || echo ERROR && exit /b
move ..\Compiled\%1\net48\GL\*.dylib ..\Compiled\%1\net48\Resources\Binaries\OpenGL || echo ERROR && exit /b
xcopy ..\Compiled\%1\net48\GL\x86 ..\Compiled\%1\net48\Resources\Binaries\OpenGL\x86\ /e || echo ERROR && exit /b
xcopy ..\Compiled\%1\net48\GL\x64 ..\Compiled\%1\net48\Resources\Binaries\OpenGL\x64\ /e || echo ERROR && exit /b
ren ..\Compiled\%1\net48\Resources\DXMainClient.exe.config clientogl.exe.config || echo ERROR && exit /b
ren ..\Compiled\%1\net48\Resources\DXMainClient.exe clientogl.exe || echo ERROR && exit /b
ren ..\Compiled\%1\net48\Resources\DXMainClient.pdb clientogl.pdb || echo ERROR && exit /b
rd /s /q ..\Compiled\%1\net48\GL || echo ERROR && exit /b

for /f "delims=" %%a in ('echo Structuring %1 .NET4.8 XNA Files') do ( echo %%a & title %%a )
move ..\Compiled\%1\net48\XNA\DXMainClient.* ..\Compiled\%1\net48\Resources || echo ERROR && exit /b
move ..\Compiled\%1\net48\XNA\Microsoft.Xna.* ..\Compiled\%1\net48\Resources\Binaries\XNA || echo ERROR && exit /b
move ..\Compiled\%1\net48\XNA\Rampastring.XNAUI.XNA.* ..\Compiled\%1\net48\Resources\Binaries\XNA || echo ERROR && exit /b
move ..\Compiled\%1\net48\XNA\ClientCore.* ..\Compiled\%1\net48\Resources\Binaries\XNA || echo ERROR && exit /b
move ..\Compiled\%1\net48\XNA\ClientGUI.* ..\Compiled\%1\net48\Resources\Binaries\XNA || echo ERROR && exit /b
move ..\Compiled\%1\net48\XNA\DTAConfig.* ..\Compiled\%1\net48\Resources\Binaries\XNA || echo ERROR && exit /b
ren ..\Compiled\%1\net48\Resources\DXMainClient.exe.config clientxna.exe.config || echo ERROR && exit /b
ren ..\Compiled\%1\net48\Resources\DXMainClient.exe clientxna.exe || echo ERROR && exit /b
ren ..\Compiled\%1\net48\Resources\DXMainClient.pdb clientxna.pdb || echo ERROR && exit /b
rd /s /q ..\Compiled\%1\net48\XNA || echo ERROR && exit /b

for /f "delims=" %%a in ('echo %1 .NET4.8 published to \Compiled\%1\net48') do ( echo %%a & title %%a )