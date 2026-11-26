@echo off
setlocal EnableExtensions

rem ============================================================
rem  SMAPI Loader build/install helper
rem
rem  Usage:
rem    tools.cmd build <dev^|release>      build only
rem    tools.cmd install <dev^|release>    install existing APK
rem    tools.cmd dev                       build Dev, then install
rem    tools.cmd release                   build Release, then install
rem    tools.cmd help
rem
rem  Extra args after the command are forwarded to "dotnet build", e.g.
rem    tools.cmd build dev -v:n
rem
rem  Environment (auto-detected when possible):
rem    ANDROID_HOME / ANDROID_SDK_ROOT  -> Android SDK
rem    JAVA_HOME                        -> JDK (falls back to newest jdk-21*/jdk-17*)
rem    ANDROID_SERIAL                   -> pick a device when several are attached
rem ============================================================

set "ROOT=%~dp0"

if /i "%~1"=="" goto :usage
if /i "%~1"=="-h" goto :usage
if /i "%~1"=="--help" goto :usage
if /i "%~1"=="help" goto :usage

set "ACTION=%~1"
set "TARGET=%~2"

rem shorthands
if /i "%ACTION%"=="dev" (
    set "ACTION=all"
    set "TARGET=dev"
)
if /i "%ACTION%"=="release" (
    set "ACTION=all"
    set "TARGET=release"
)

if /i "%TARGET%"=="dev" (
    set "CONFIG=Dev"
) else if /i "%TARGET%"=="release" (
    set "CONFIG=Release"
) else (
    echo [tools] unknown target "%TARGET%" ^(expected: dev ^| release^)
    goto :usage
)

if /i "%ACTION%"=="build" goto :resolve_env
if /i "%ACTION%"=="install" goto :resolve_env
if /i "%ACTION%"=="all" goto :resolve_env
echo [tools] unknown command "%ACTION%"
goto :usage

:resolve_env
set "SDK=%ANDROID_HOME%"
if not defined SDK set "SDK=%ANDROID_SDK_ROOT%"
if not defined SDK set "SDK=%LOCALAPPDATA%\Android\Sdk"

set "JDK=%JAVA_HOME%"
if not defined JDK for /d %%D in ("C:\Program Files\Eclipse Adoptium\jdk-21*") do set "JDK=%%D"
if not defined JDK for /d %%D in ("C:\Program Files\Eclipse Adoptium\jdk-17*") do set "JDK=%%D"

set "ADB=%SDK%\platform-tools\adb.exe"
if not exist "%ADB%" set "ADB=adb"

rem collect remaining args (%3..%9) to forward to dotnet build
set "EXTRA="
shift
shift
:collect_extra
if "%~1"=="" goto :collected
set "EXTRA=%EXTRA% %1"
shift
goto :collect_extra
:collected

if /i "%ACTION%"=="all" set "DO_INSTALL=1"
if /i "%ACTION%"=="install" goto :do_install

:do_build
echo.
echo [tools] build %CONFIG%
echo [tools] SDK: %SDK%
echo [tools] JDK: %JDK%
if not exist "%SDK%" (
    echo [tools] Android SDK not found at "%SDK%" ^(set ANDROID_HOME or ANDROID_SDK_ROOT^)
    exit /b 1
)
call dotnet build "%ROOT%SMAPIGameLoader\SMAPIGameLoader.csproj" -c "%CONFIG%" -p:AndroidSdkDirectory="%SDK%" -p:JavaSdkDirectory="%JDK%" --nologo%EXTRA%
if errorlevel 1 (
    echo [tools] build %CONFIG% failed
    exit /b 1
)
if not defined DO_INSTALL exit /b 0

:do_install
set "APK=%ROOT%SMAPIGameLoader\bin\%CONFIG%\net9.0-android\abc.smapi.gameloader-Signed.apk"
if not exist "%APK%" (
    echo [tools] APK not found: "%APK%"
    echo [tools] run: tools.cmd build %TARGET%
    exit /b 1
)
echo.
echo [tools] install %CONFIG%
echo [tools] APK: %APK%
rem -d allows a version downgrade (ApplicationVersion is a build timestamp), so switching
rem between a newer Dev build and an older Release build does not fail.
call "%ADB%" install -r -d "%APK%"
if errorlevel 1 (
    echo [tools] install failed
    exit /b 1
)
echo [tools] done
exit /b 0

:usage
echo.
echo  SMAPI Loader build/install helper
echo.
echo  Usage:
echo    tools.cmd build ^<dev^|release^>      build only
echo    tools.cmd install ^<dev^|release^>    install existing APK
echo    tools.cmd dev                       build Dev, then install
echo    tools.cmd release                   build Release, then install
echo    tools.cmd help
echo.
echo  Examples:
echo    tools.cmd build dev -v:n
echo    tools.cmd dev
echo.
echo  Env: ANDROID_HOME/ANDROID_SDK_ROOT, JAVA_HOME, ANDROID_SERIAL
exit /b 1
