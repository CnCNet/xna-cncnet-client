rem @echo off

SET msbuild=""

for /f "usebackq tokens=1* delims=: " %%i in (`"%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe" -latest -requires Microsoft.Component.MSBuild`) do (
  if /i "%%i"=="installationPath" set InstallDir=%%j
)

if exist "%InstallDir%\MSBuild\15.0\Bin\MSBuild.exe" (
  SET "msbuild=%InstallDir%\MSBuild\15.0\Bin\MSBuild.exe"
)

ENDLOCAL&set msbuild=%msbuild%
