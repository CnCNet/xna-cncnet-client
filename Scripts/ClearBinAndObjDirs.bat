@echo off

cd /d %~dp0
cd ..

for /f "tokens=*" %%f in ('dir ".\" /a:d /b') do (
	rmdir /q /s "%%f\bin" > nul 2> nul
	rmdir /q /s "%%f\obj" > nul 2> nul
)
