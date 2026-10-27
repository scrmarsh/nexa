@echo off
cd /d "%~dp0"
dotnet build ".\SessionDashboard.csproj" -c Release
if errorlevel 1 (
    pause
    exit /b 1
)
start "" "%~dp0bin\Release\net10.0-windows\Nexa.exe"
