@echo off
rem Compiles the client with a specific configuration for a specific platform

if [%1]==[] goto argfail
if [%2]==[] goto argfail

set configuration=%1
set platform=%2

call FindMSBuild

if exist "%msbuild%" goto msbuildok
ECHO.
ECHO.
echo Visual Studio 2017, Visual Studio 2019 or MSBuild required.
ECHO.
ECHO.
goto error

:msbuildok

ECHO.
echo Compiling %configuration% %platform%
ECHO.

"%msbuild%" ..\DXClient.sln /t:Rebuild /p:Platform=%platform% /p:Configuration=%configuration%
if errorlevel 1 goto error

ECHO.
echo Compiled succesfully.
ECHO.

goto end

:argfail
echo Syntax: %0% (configuration) (platform) (example: %0% DTARelease SharpDX)
goto error

:error
endlocal
exit /B 1

:end
endlocal
