@echo off
setlocal

set "EXE=%~dp0Builds\Windows\AsylumHorrorPrototype.exe"
if not exist "%EXE%" (
  echo EXE not found: %EXE%
  pause
  exit /b 1
)

set "PORT=7777"
if not "%~1"=="" set "PORT=%~1"

echo Launching host window...
start "" "%EXE%" --host --port=%PORT% -window-mode windowed -screen-width 1280 -screen-height 720
timeout /t 3 >nul

echo Launching client window...
start "" "%EXE%" --join=127.0.0.1:%PORT% -window-mode windowed -screen-width 1280 -screen-height 720
exit /b 0
