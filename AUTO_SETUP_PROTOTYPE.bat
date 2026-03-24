@echo off
setlocal

set "SCRIPT_DIR=%~dp0"
set "PS_SCRIPT=%SCRIPT_DIR%automation\run_full_prototype.ps1"

if not exist "%PS_SCRIPT%" (
  echo [Horror-AutoSetup] Missing script: %PS_SCRIPT%
  pause
  exit /b 1
)

echo [Horror-AutoSetup] Starting full automatic setup...
powershell -NoProfile -ExecutionPolicy Bypass -File "%PS_SCRIPT%" -BuildExe 1

if errorlevel 1 (
  echo [Horror-AutoSetup] FAILED. Check logs in "%SCRIPT_DIR%automation\".
  pause
  exit /b 1
)

echo [Horror-AutoSetup] SUCCESS. Prototype + EXE are ready.
pause
exit /b 0
