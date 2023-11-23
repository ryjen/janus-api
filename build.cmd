dotnet restore
dotnet tool install -g Amazon.Lambda.Tools --framework net6.0
dotnet lambda package --configuration Release --framework net6.0 --output-package bin/Release/net6.0/auth.zip --cmd auth
dotnet lambda package --configuration Release --framework net6.0 --output-package bin/Release/net6.0/account.zip --cmd account
