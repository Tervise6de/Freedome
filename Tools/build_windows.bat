@echo off
REM Builds the Windows 64-bit player headlessly on Windows.
REM
REM   Tools\build_windows.bat [--regenerate] [--bake] [--development]
REM
REM Set UNITY_PATH to the editor executable if it is not in the default Unity
REM Hub location, for example:
REM   set UNITY_PATH=C:\Program Files\Unity\Hub\Editor\6000.0.58f1\Editor\Unity.exe

setlocal enabledelayedexpansion

set "PROJECT_ROOT=%~dp0.."
set "LOG_DIR=%PROJECT_ROOT%\Builds\Windows"
set "LOG_FILE=%LOG_DIR%\build.log"
set "EXTRA_ARGS="

:parse
if "%~1"=="" goto parsed
if /I "%~1"=="--regenerate"  set "EXTRA_ARGS=!EXTRA_ARGS! -regenerate"
if /I "%~1"=="--bake"        set "EXTRA_ARGS=!EXTRA_ARGS! -bakeLighting"
if /I "%~1"=="--development" set "EXTRA_ARGS=!EXTRA_ARGS! -development"
shift
goto parse
:parsed

if not defined UNITY_PATH (
  for /f "delims=" %%D in ('dir /b /ad /o-n "C:\Program Files\Unity\Hub\Editor" 2^>nul') do (
    if not defined UNITY_PATH set "UNITY_PATH=C:\Program Files\Unity\Hub\Editor\%%D\Editor\Unity.exe"
  )
)

if not exist "%UNITY_PATH%" (
  echo Could not find a Unity editor. Install Unity 6 with Windows Build Support,
  echo or set UNITY_PATH to the editor executable.
  exit /b 1
)

echo Unity:   %UNITY_PATH%
echo Project: %PROJECT_ROOT%

if not exist "%LOG_DIR%" mkdir "%LOG_DIR%"

"%UNITY_PATH%" -quit -batchmode -nographics ^
  -projectPath "%PROJECT_ROOT%" ^
  -executeMethod Freedome.EditorTools.Build.WindowsBuild.PerformBuild ^
  -logFile "%LOG_FILE%" %EXTRA_ARGS%

if errorlevel 1 (
  echo Build failed. See "%LOG_FILE%".
  exit /b 1
)

echo.
echo Build succeeded: Builds\Windows\ShedRoomDemo\ShedRoomDemo.exe
endlocal
