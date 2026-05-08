#!/bin/bash

# 1. Define Variables
PROJECT_NAME="RonCafeLaucher.Desktop" # Change this to your actual project name
OUTPUT_DIR="./publish"
RUNTIME="linux-x64" # Use linux-arm64 for Raspberry Pi/ARM devices

echo "Cleaning old builds..."
rm -rf $OUTPUT_DIR

echo "Compiling and Publishing Avalonia App for $RUNTIME..."

# 2. Run the Publish Command
# -c Release: Builds in Release mode
# -r $RUNTIME: Targets Linux
# --self-contained: Includes the .NET runtime (user doesn't need .NET installed)
# -p:PublishSingleFile=true: Bundles everything into one executable
dotnet publish $PROJECT_NAME.csproj \
    -c Release \
    -r $RUNTIME \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    -o $OUTPUT_DIR

if [ $? -eq 0 ]; then
    echo "Successfully compiled! Files are in $OUTPUT_DIR"
else
    echo "Compilation failed."
fi
