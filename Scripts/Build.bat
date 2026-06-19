@echo off
where pwsh > nul 2> nul
if %errorlevel% equ 0 (
  pwsh -ExecutionPolicy Bypass -File build.ps1
) else (
  echo "Please install PowerShell 7."
)
pause
