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

echo Starting host on port %PORT%...
start "" "%EXE%" --host --port=%PORT%
exit /b 0
