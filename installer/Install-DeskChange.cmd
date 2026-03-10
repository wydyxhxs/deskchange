@echo off
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Install-DeskChange.ps1"
exit /b %errorlevel%
