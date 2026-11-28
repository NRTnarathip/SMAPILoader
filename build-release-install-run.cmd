@echo off
rem Helper: build Release (build-release.cmd names the artifact), install it, then launch.
call "%~dp0build-release.cmd" %*
if errorlevel 1 exit /b %ERRORLEVEL%

call "%~dp0tools.cmd" install release
if errorlevel 1 exit /b %ERRORLEVEL%

call "%~dp0tools.cmd" run
exit /b %ERRORLEVEL%
