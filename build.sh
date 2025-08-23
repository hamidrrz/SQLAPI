#!/bin/bash

# SQLAPI - Build Script for Linux/Mac

echo "========================================"
echo "  SQLAPI - Build Script"
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

echo "Building SQLAPI for deployment..."
echo

# Clean previous builds
echo "Cleaning previous builds..."
if [ -d "publish" ]; then
    rm -rf "publish"
fi
echo

# Publish the application
echo "Publishing application..."
dotnet publish -c Release -o publish
echo

if [ $? -eq 0 ]; then
    echo "Build successful!"
    echo
    echo "The published application is located in the 'publish' folder."
    echo
    echo "To run the application:"
    echo "  1. Navigate to the 'publish' folder"
    echo "  2. Run: dotnet SQLAPI.dll"
    echo
else
    echo "Build failed!"
    echo
fi

read -p "Press enter to continue..."