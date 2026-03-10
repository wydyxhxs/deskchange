@echo off
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Uninstall-DeskChange.ps1"
exit /b %errorlevel%
