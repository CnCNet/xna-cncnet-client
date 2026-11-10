@echo off
REM Author: frg2089

setlocal

REM Check for pwsh.
for %%e in (pwsh) do (
  for /f "delims=" %%i in ('where %%e 2^>nul') do (
    set "ps_path=%%i"
    goto found
  )
)

REM No PowerShell executable found.
echo Error: Unable to find command pwsh. Please make sure PowerShell 7 is installed. >&2
exit /b 1

:found
echo Found PowerShell at: "%ps_path%"

REM Pass the value outside of setlocal scope.
endlocal & set "ps_path=%ps_path%"

REM Run ps1 in this directory and forward all arguments.
"%ps_path%" -ExecutionPolicy Bypass -File "%~dp0%~n0.ps1" %*
set "exit_code=%errorlevel%"
pause
exit /b %exit_code%
