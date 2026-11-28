@echo off
rem Bump the launcher version (SMAPIGameLoader.csproj ApplicationDisplayVersion).
rem   bump-version.cmd major         1.1.7 -> 2.0.0
rem   bump-version.cmd minor         1.1.7 -> 1.2.0
rem   bump-version.cmd patch         1.1.7 -> 1.1.8
rem   bump-version.cmd 2.0.0         set an exact version
if "%~1"=="" (
    echo Usage: bump-version.cmd ^<major^|minor^|patch^|x.y.z^>
    exit /b 1
)
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0bump-version.ps1" "%~1"
exit /b %ERRORLEVEL%
