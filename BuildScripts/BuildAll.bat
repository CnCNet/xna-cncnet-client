@echo off
call BuildYR || echo ERROR && exit /b
call BuildTS || echo ERROR && exit /b
call BuildAres || echo ERROR && exit /b