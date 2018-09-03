@echo off
rem Compiles the client for a specific configuration (game) for all platforms.

if [%1]==[] goto argfail
set configuration=%1

echo Compiling client for %configuration%

call Build %configuration% SharpDX
call Build %configuration% WindowsGL
call Build %configuration% XNAFramework

if errorlevel 1 goto error

call CopyCompiled

echo Compiling complete.
pause

goto end

:argfail
echo Syntax: %0% (configuration) (example: %0% DTARelease)
goto error

:error
endlocal
exit /B 1

:end
endlocal
