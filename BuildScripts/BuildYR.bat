@echo off
call Build.NET6.0 YR || echo ERROR && exit /b
call Build.NET4.8 YR || echo ERROR && exit /b