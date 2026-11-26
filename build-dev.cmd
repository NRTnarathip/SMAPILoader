@echo off
rem Kept for compatibility: forwards to tools.cmd (same as `tools.cmd build dev`).
call "%~dp0tools.cmd" build dev %*
exit /b %ERRORLEVEL%
