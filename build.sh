#!/bin/bash

FUNCTIONS="auth account player generic"

DOTNET_VERSION="net6.0"

#install zip on debian OS, since microsoft/dotnet container doesn't have zip by default
if [ -f /etc/debian_version ]; then
	echo "update system"
	apt -qq update
	apt -qq -y install zip
fi

echo "Check lambda tools installed"
dotnet tool install -g Amazon.Lambda.Tools --framework $DOTNET_VERSION 2>/dev/null

echo "Restore project"
dotnet restore

for func in $FUNCTIONS; do
	echo "Package $func"
	dotnet lambda package --configuration Release --framework $DOTNET_VERSION --output-package bin/Release/"$DOTNET_VERSION"/"$func".zip --project-location "$func" || exit
done
