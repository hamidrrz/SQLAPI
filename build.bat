@echo off
title SQLAPI - Build Script

echo ========================================
echo   SQLAPI - Build Script
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

echo Building SQLAPI for deployment...
echo.

REM Clean previous builds
echo Cleaning previous builds...
if exist "publish" rmdir /s /q "publish"
echo.

REM Publish the application
echo Publishing application...
dotnet publish -c Release -o publish
echo.

if %errorlevel% equ 0 (
    echo Build successful!
    echo.
    echo The published application is located in the 'publish' folder.
    echo.
    echo To run the application:
    echo   1. Navigate to the 'publish' folder
    echo   2. Run: dotnet SQLAPI.dll
    echo.
) else (
    echo Build failed!
    echo.
)

pause