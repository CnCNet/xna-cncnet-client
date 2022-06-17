@echo off
set configuration=%1
if [%1]==[] set configuration=Release

call BuildYR %configuration% || echo ERROR && exit /b
call BuildTS %configuration% || echo ERROR && exit /b
call BuildAres %configuration% || echo ERROR && exit /b