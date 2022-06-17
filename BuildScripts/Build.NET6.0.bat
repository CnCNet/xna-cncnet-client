@echo off
set configuration=%2
if [%1]==[] echo Missing game parameter (YR, TS or Ares) && exit /b
if [%2]==[] set configuration=Release

for /f "delims=" %%a in ('echo Creating %1 .NET6.0 Folders') do ( echo %%a & title %%a )
if exist ..\Compiled\%1\net60\ ( rd /s /q ..\Compiled\%1\net60 || echo ERROR && exit /b )
mkdir ..\Compiled\%1\net60\Resources\Binaries\Windows || echo ERROR && exit /b
mkdir ..\Compiled\%1\net60\Resources\Binaries\OpenGL || echo ERROR && exit /b
mkdir ..\Compiled\%1\net60\Resources\Binaries\XNA || echo ERROR && exit /b

for /f "delims=" %%a in ('echo Publish %1DX%configuration% .NET6.0') do ( echo %%a & title %%a )
dotnet publish ..\DXMainClient\DXMainClient.csproj --no-self-contained -c %1DX%configuration% -o ..\Compiled\%1\net60\DX -f net6.0-windows10.0.22000.0 || echo ERROR && exit /b
for /f "delims=" %%a in ('echo Publish %1GL%configuration% .NET6.0') do ( echo %%a & title %%a )
dotnet publish ..\DXMainClient\DXMainClient.csproj --no-self-contained -c %1GL%configuration% -o ..\Compiled\%1\net60\GL -f net6.0-windows10.0.22000.0 || echo ERROR && exit /b
for /f "delims=" %%a in ('echo Publish %1XNA%configuration% .NET6.0') do ( echo %%a & title %%a )
dotnet publish ..\DXMainClient\DXMainClient.csproj --no-self-contained -c %1XNA%configuration% -o ..\Compiled\%1\net60\XNA -f net6.0-windows10.0.22000.0 -r win-x86 || echo ERROR && exit /b

for /f "delims=" %%a in ('echo Structuring %1 .NET6.0 Main Files') do ( echo %%a & title %%a )
xcopy ..\Compiled\%1\net60\DX\*.* ..\Compiled\%1\net60\Resources\Binaries /e || echo ERROR && exit /b
rd /s /q ..\Compiled\%1\net60\DX || echo ERROR && exit /b

for /f "delims=" %%a in ('echo Structuring %1 .NET6.0 DirectX Files') do ( echo %%a & title %%a )
move ..\Compiled\%1\net60\Resources\Binaries\DXMainClient.* ..\Compiled\%1\net60\Resources\Binaries\Windows || echo ERROR && exit /b
move ..\Compiled\%1\net60\Resources\Binaries\Windows\DXMainClient.exe ..\Compiled\%1\net60\Resources || echo ERROR && exit /b
move ..\Compiled\%1\net60\Resources\Binaries\SharpDX.* ..\Compiled\%1\net60\Resources\Binaries\Windows || echo ERROR && exit /b
move ..\Compiled\%1\net60\Resources\Binaries\MonoGame.* ..\Compiled\%1\net60\Resources\Binaries\Windows || echo ERROR && exit /b
move ..\Compiled\%1\net60\Resources\Binaries\Rampastring.XNAUI.DX.* ..\Compiled\%1\net60\Resources\Binaries\Windows || echo ERROR && exit /b
move ..\Compiled\%1\net60\Resources\Binaries\ClientCore.* ..\Compiled\%1\net60\Resources\Binaries\Windows || echo ERROR && exit /b
move ..\Compiled\%1\net60\Resources\Binaries\ClientGUI.* ..\Compiled\%1\net60\Resources\Binaries\Windows || echo ERROR && exit /b
move ..\Compiled\%1\net60\Resources\Binaries\DTAConfig.* ..\Compiled\%1\net60\Resources\Binaries\Windows || echo ERROR && exit /b
xcopy ..\Compiled\%1\net60\Resources\Binaries\runtimes ..\Compiled\%1\net60\Resources\Binaries\Windows\runtimes\ /e || echo ERROR && exit /b
rd /s /q ..\Compiled\%1\net60\Resources\Binaries\runtimes || echo ERROR && exit /b
ren ..\Compiled\%1\net60\Resources\Binaries\Windows\DXMainClient.runtimeconfig.json clientdx.runtimeconfig.json || echo ERROR && exit /b
ren ..\Compiled\%1\net60\Resources\Binaries\Windows\DXMainClient.deps.json clientdx.deps.json || echo ERROR && exit /b
ren ..\Compiled\%1\net60\Resources\Binaries\Windows\DXMainClient.dll clientdx.dll || echo ERROR && exit /b
ren ..\Compiled\%1\net60\Resources\Binaries\Windows\DXMainClient.dll.config clientdx.dll.config || echo ERROR && exit /b
ren ..\Compiled\%1\net60\Resources\Binaries\Windows\DXMainClient.pdb clientdx.pdb || echo ERROR && exit /b
ren ..\Compiled\%1\net60\Resources\DXMainClient.exe clientdx.exe || echo ERROR && exit /b

for /f "delims=" %%a in ('echo Structuring %1 .NET6.0 OpenGL Files') do ( echo %%a & title %%a )
move ..\Compiled\%1\net60\GL\DXMainClient.* ..\Compiled\%1\net60\Resources\Binaries\OpenGL || echo ERROR && exit /b
move ..\Compiled\%1\net60\Resources\Binaries\OpenGL\DXMainClient.exe ..\Compiled\%1\net60\Resources || echo ERROR && exit /b
move ..\Compiled\%1\net60\GL\MonoGame.* ..\Compiled\%1\net60\Resources\Binaries\OpenGL || echo ERROR && exit /b
move ..\Compiled\%1\net60\GL\Rampastring.XNAUI.GL.* ..\Compiled\%1\net60\Resources\Binaries\OpenGL || echo ERROR && exit /b
move ..\Compiled\%1\net60\GL\ClientCore.* ..\Compiled\%1\net60\Resources\Binaries\OpenGL || echo ERROR && exit /b
move ..\Compiled\%1\net60\GL\ClientGUI.* ..\Compiled\%1\net60\Resources\Binaries\OpenGL || echo ERROR && exit /b
move ..\Compiled\%1\net60\GL\DTAConfig.* ..\Compiled\%1\net60\Resources\Binaries\OpenGL || echo ERROR && exit /b
xcopy ..\Compiled\%1\net60\GL\runtimes ..\Compiled\%1\net60\Resources\Binaries\OpenGL\runtimes\ /e || echo ERROR && exit /b
ren ..\Compiled\%1\net60\Resources\Binaries\OpenGL\DXMainClient.runtimeconfig.json clientogl.runtimeconfig.json || echo ERROR && exit /b
ren ..\Compiled\%1\net60\Resources\Binaries\OpenGL\DXMainClient.deps.json clientogl.deps.json || echo ERROR && exit /b
ren ..\Compiled\%1\net60\Resources\Binaries\OpenGL\DXMainClient.dll clientogl.dll || echo ERROR && exit /b
ren ..\Compiled\%1\net60\Resources\Binaries\OpenGL\DXMainClient.dll.config clientogl.dll.config || echo ERROR && exit /b
ren ..\Compiled\%1\net60\Resources\Binaries\OpenGL\DXMainClient.pdb clientogl.pdb || echo ERROR && exit /b
ren ..\Compiled\%1\net60\Resources\DXMainClient.exe clientogl.exe || echo ERROR && exit /b
rd /s /q ..\Compiled\%1\net60\GL || echo ERROR && exit /b

for /f "delims=" %%a in ('echo Structuring %1 .NET6.0 XNA Files') do ( echo %%a & title %%a )
move ..\Compiled\%1\net60\XNA\DXMainClient.* ..\Compiled\%1\net60\Resources\Binaries\XNA || echo ERROR && exit /b
move ..\Compiled\%1\net60\Resources\Binaries\XNA\DXMainClient.exe ..\Compiled\%1\net60\Resources || echo ERROR && exit /b
move ..\Compiled\%1\net60\XNA\Microsoft.Xna.* ..\Compiled\%1\net60\Resources\Binaries\XNA || echo ERROR && exit /b
move ..\Compiled\%1\net60\XNA\Rampastring.XNAUI.XNA.* ..\Compiled\%1\net60\Resources\Binaries\XNA || echo ERROR && exit /b
move ..\Compiled\%1\net60\XNA\ClientCore.* ..\Compiled\%1\net60\Resources\Binaries\XNA || echo ERROR && exit /b
move ..\Compiled\%1\net60\XNA\ClientGUI.* ..\Compiled\%1\net60\Resources\Binaries\XNA || echo ERROR && exit /b
move ..\Compiled\%1\net60\XNA\DTAConfig.* ..\Compiled\%1\net60\Resources\Binaries\XNA || echo ERROR && exit /b
ren ..\Compiled\%1\net60\Resources\Binaries\XNA\DXMainClient.runtimeconfig.json clientxna.runtimeconfig.json || echo ERROR && exit /b
ren ..\Compiled\%1\net60\Resources\Binaries\XNA\DXMainClient.deps.json clientxna.deps.json || echo ERROR && exit /b
ren ..\Compiled\%1\net60\Resources\Binaries\XNA\DXMainClient.dll clientxna.dll || echo ERROR && exit /b
ren ..\Compiled\%1\net60\Resources\Binaries\XNA\DXMainClient.dll.config clientxna.dll.config || echo ERROR && exit /b
ren ..\Compiled\%1\net60\Resources\Binaries\XNA\DXMainClient.pdb clientxna.pdb || echo ERROR && exit /b
ren ..\Compiled\%1\net60\Resources\DXMainClient.exe clientxna.exe || echo ERROR && exit /b
rd /s /q ..\Compiled\%1\net60\XNA || echo ERROR && exit /b

for /f "delims=" %%a in ('echo Patching %1 AppHosts') do ( echo %%a & title %%a )
..\AdditionalFiles\AppHostPatcher\AppHostPatcher.exe ..\Compiled\%1\net60\Resources\clientdx.exe DXMainClient.dll Binaries\Windows\clientdx.dll || echo ERROR && exit /b
..\AdditionalFiles\AppHostPatcher\AppHostPatcher.exe ..\Compiled\%1\net60\Resources\clientogl.exe DXMainClient.dll Binaries\OpenGL\clientogl.dll || echo ERROR && exit /b
..\AdditionalFiles\AppHostPatcher\AppHostPatcher.exe ..\Compiled\%1\net60\Resources\clientxna.exe DXMainClient.dll Binaries\XNA\clientxna.dll || echo ERROR && exit /b

for /f "delims=" %%a in ('echo %1 .NET6.0 published to \Compiled\%1\net60') do ( echo %%a & title %%a )