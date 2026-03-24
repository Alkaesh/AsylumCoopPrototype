@echo off
setlocal

set "SCRIPT_DIR=%~dp0"
set "PS_SCRIPT=%SCRIPT_DIR%automation\run_full_prototype.ps1"

if not exist "%PS_SCRIPT%" exit /b 1

powershell -NoProfile -ExecutionPolicy Bypass -File "%PS_SCRIPT%" -BuildExe 1
exit /b %errorlevel%
