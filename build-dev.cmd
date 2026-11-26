@echo off
setlocal

rem Fast dev build: configuration "Dev" (arm64 only, assembly store, no trim/AOT/analyzers).
rem Usage: build-dev.cmd [extra dotnet build args, e.g. -v:n]

set "SDK=%ANDROID_HOME%"
if not defined SDK set "SDK=%ANDROID_SDK_ROOT%"
if not defined SDK set "SDK=%LOCALAPPDATA%\Android\Sdk"

set "JDK=%JAVA_HOME%"
if not defined JDK for /d %%D in ("C:\Program Files\Eclipse Adoptium\jdk-21*") do set "JDK=%%D"

dotnet build "%~dp0SMAPIGameLoader\SMAPIGameLoader.csproj" -c "Dev" -p:AndroidSdkDirectory="%SDK%" -p:JavaSdkDirectory="%JDK%" --nologo %*

exit /b %ERRORLEVEL%
