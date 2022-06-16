@echo off
call Build.NET6.0 Ares || echo ERROR && exit /b
call Build.NET4.8 Ares || echo ERROR && exit /b