dotnet tool install -g Amazon.Lambda.Tools --framework net10.0
dotnet restore

dotnet lambda package --configuration Release --framework net10.0 --output-package bin/Release/net10.0/auth.zip --project-location auth

dotnet lambda package --configuration Release --framework net10.0 --output-package bin/Release/net10.0/account.zip --project-location account

dotnet lambda package --configuration Release --framework net10.0 --output-package bin/Release/net10.0/player.zip --project-location player

dotnet lambda package --configuration Release --framework net10.0 --output-package bin/Release/net10.0/data.zip --project-location data
