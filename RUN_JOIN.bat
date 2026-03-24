@echo off
setlocal

set "EXE=%~dp0Builds\Windows\AsylumHorrorPrototype.exe"
if not exist "%EXE%" (
  echo EXE not found: %EXE%
  pause
  exit /b 1
)

set "TARGET=%~1"
if "%TARGET%"=="" (
  set /p TARGET=Enter host IP or VPN IP (example 26.10.20.30:7777): 
)

if "%TARGET%"=="" (
  echo Empty target.
  pause
  exit /b 1
)

echo Connecting to %TARGET%...
start "" "%EXE%" --join=%TARGET%
exit /b 0
