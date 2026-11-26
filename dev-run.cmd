@echo off
rem Build Dev, install it, then launch the app.
call "%~dp0tools.cmd" dev %*
if errorlevel 1 exit /b %ERRORLEVEL%

call "%~dp0tools.cmd" run
exit /b %ERRORLEVEL%
