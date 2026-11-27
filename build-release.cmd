@echo off
call "%~dp0tools.cmd" build release %*
exit /b %ERRORLEVEL%