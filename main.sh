#!/usr/bin/env bash

export PATH="$PATH:$PWD/node_modules/.bin"

type dotnet >/dev/null 2>&1

if [ $? -ne 0 ]; then
  echo ""
  echo "Dotnet command not found."
  echo "Please install dotnet core for your operating system."
  echo ""
  exit 1
fi

type npm >/dev/null 2>&1

if [ $? -ne 0 ]; then
  echo ""
  echo "Node package manager not found."
  echo "Please install NPM for your operating system."
  echo ""
  exit 1
fi

read -p "Set API key? (y/N) " setApiKey

case "$setApiKey" in

  y|Y*)
    API_KEY=$(tr -dc 'A-F0-9' < /dev/urandom | head -c32)
    echo "API_KEY=$API_KEY" > .env
    ;;
esac


read -p "Install node packages? (y/N) " npmInstall

case "$npmInstall" in
  y|Y*)
    npm install
    ;;
esac

read -p "Build project? (y/N) " buildProject

case "$buildProject" in
  y|Y*)
    ./build.sh
    ;;
esac

read -p "Deploy to AWS? (y/N) " deployProject

case "$deployProject" in
  y|Y*)
    serverless deploy
    ;;
esac

echo ""
echo "Done."
