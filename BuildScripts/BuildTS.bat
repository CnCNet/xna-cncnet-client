@echo off
call Build.NET6.0 TS || echo ERROR && exit /b
call Build.NET4.8 TS || echo ERROR && exit /b