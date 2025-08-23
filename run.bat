@echo off
title SQLAPI - MySQL Query REST API Service

echo ========================================
echo   SQLAPI - MySQL Query REST API Service
echo ========================================
echo.

REM Check if dotnet is installed
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: .NET 9 SDK is not installed or not in PATH
    echo Please install .NET 9 SDK from https://dotnet.microsoft.com/download/dotnet/9.0
    echo.
    pause
    exit /b 1
)

REM Check if appsettings.json exists
if not exist "appsettings.json" (
    echo ERROR: appsettings.json not found
    echo Please ensure you have the configuration file in the application directory
    echo.
    pause
    exit /b 1
)

echo Starting SQLAPI...
echo.
echo The application will be available at:
echo   http://localhost:5000
echo   https://localhost:5001
echo.
echo Press Ctrl+C to stop the application
echo.

REM Run the application
dotnet run

echo.
echo Application stopped
pause