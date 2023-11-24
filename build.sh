#!/bin/bash

FUNCTIONS="auth account"

#install zip on debian OS, since microsoft/dotnet container doesn't have zip by default
if [ -f /etc/debian_version ]; then
	apt -qq update
	apt -qq -y install zip
fi

dotnet tool install -g Amazon.Lambda.Tools --framework net6.0 2>/dev/null

dotnet restore

for func in $FUNCTIONS; do
	dotnet lambda package --configuration Release --framework net6.0 --output-package bin/Release/net6.0/"$func".zip --project-location "$func" || exit
done
