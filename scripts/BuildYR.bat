@echo off
where pwsh > nul 2> nul
if %errorlevel% equ 0 (
  pwsh -ExecutionPolicy Bypass -File build.ps1 -Games YR
) else (
  echo "Please Install PowerShell."
  echo "https://aka.ms/pscore6"
)