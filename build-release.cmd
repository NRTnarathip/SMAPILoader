@echo off
setlocal EnableExtensions

rem ============================================================
rem  Build the Release APK and keep a copy named
rem    SMAPI-Launcher-<version>-(<buildCode>).apk
rem
rem  <buildCode> is the APK versionCode (build timestamp, same idea as
rem  the SMAPI zip build code), read back from the built APK with aapt.
rem  The named copy lands in Artifacts\Release\ (git-ignored); older copies
rem  there are deleted first, since each build gets a new build code.
rem
rem  Extra args are forwarded to "dotnet build", e.g.
rem    build-release.cmd -v:n
rem ============================================================

set "ROOT=%~dp0"
set "APK=%ROOT%SMAPIGameLoader\bin\Release\net9.0-android\abc.smapi.gameloader-Signed.apk"

call "%ROOT%tools.cmd" build release %*
if errorlevel 1 exit /b 1

set "SDK=%ANDROID_HOME%"
if not defined SDK set "SDK=%ANDROID_SDK_ROOT%"
if not defined SDK set "SDK=%LOCALAPPDATA%\Android\Sdk"

set "AAPT="
for /f "delims=" %%D in ('dir /b /ad /o-n "%SDK%\build-tools" 2^>nul') do (
    if not defined AAPT if exist "%SDK%\build-tools\%%D\aapt.exe" set "AAPT=%SDK%\build-tools\%%D\aapt.exe"
)
if not defined AAPT (
    echo [release] aapt.exe not found under "%SDK%\build-tools", APK left under its default name
    exit /b 0
)

set "BADGING=%TEMP%\smapi_launcher_badging.txt"
"%AAPT%" dump badging "%APK%" 2>nul | findstr /b "package:" > "%BADGING%"
set "APP_VERSION="
set "BUILD_CODE="
for /f "tokens=4,6 delims='" %%A in ('type "%BADGING%"') do (
    set "BUILD_CODE=%%A"
    set "APP_VERSION=%%B"
)
del "%BADGING%" >nul 2>&1

if not defined BUILD_CODE (
    echo [release] could not read versionCode from the APK, name left as-is
    exit /b 0
)

set "OUT_DIR=%ROOT%Artifacts\Release"
set "ARTIFACT=%OUT_DIR%\SMAPI-Launcher-%APP_VERSION%-(%BUILD_CODE%).apk"
if not exist "%OUT_DIR%" mkdir "%OUT_DIR%"
del /q "%OUT_DIR%\*.apk" >nul 2>&1
copy /y "%APK%" "%ARTIFACT%" >nul
if errorlevel 1 (
    echo [release] copy failed: "%ARTIFACT%"
    exit /b 1
)
echo [release] artifact: "%ARTIFACT%"
exit /b 0
