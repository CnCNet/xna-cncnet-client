@echo off
set configuration=%1
if [%1]==[] set configuration=Release

call Build.NET6.0 YR %configuration% || echo ERROR && exit /b
call Build.NET4.8 YR %configuration% || echo ERROR && exit /b