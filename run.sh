#!/bin/bash

# SQLAPI - MySQL Query REST API Service
# Run script for Linux/Mac

echo "========================================"
echo "  SQLAPI - MySQL Query REST API Service"
echo "========================================"
echo

# Check if dotnet is installed
if ! command -v dotnet &> /dev/null
then
    echo "ERROR: .NET 9 SDK is not installed or not in PATH"
    echo "Please install .NET 9 SDK from https://dotnet.microsoft.com/download/dotnet/9.0"
    echo
    read -p "Press enter to continue..."
    exit 1
fi

# Check if appsettings.json exists
if [ ! -f "appsettings.json" ]; then
    echo "ERROR: appsettings.json not found"
    echo "Please ensure you have the configuration file in the application directory"
    echo
    read -p "Press enter to continue..."
    exit 1
fi

echo "Starting SQLAPI..."
echo
echo "The application will be available at:"
echo "  http://localhost:5000"
echo "  https://localhost:5001"
echo
echo "Press Ctrl+C to stop the application"
echo

# Run the application
dotnet run

echo
echo "Application stopped"
read -p "Press enter to continue..."