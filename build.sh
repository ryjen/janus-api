#!/bin/bash

FUNCTIONS="auth account player data"

DOTNET_VERSION="net10.0"

# Package prerequisites: the .NET SDK, Amazon.Lambda.Tools, and zip must be available.
command -v zip >/dev/null || { echo 'zip is required' >&2; exit 1; }

echo "Check lambda tools installed"
dotnet tool install -g Amazon.Lambda.Tools --framework $DOTNET_VERSION 2>/dev/null

echo "Restore project"
dotnet restore

for func in $FUNCTIONS; do
	echo "Package $func"
	dotnet lambda package --configuration Release --framework $DOTNET_VERSION --output-package bin/Release/"$DOTNET_VERSION"/"$func".zip --project-location "$func" || exit
done
