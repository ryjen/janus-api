@echo off
setlocal
where dotnet >nul 2>&1 || (echo .NET 10 SDK is required & exit /b 1)
where sam >nul 2>&1 || (echo AWS SAM CLI is required & exit /b 1)
dotnet restore janus.sln || exit /b 1
dotnet build janus.sln --configuration Release --no-restore || exit /b 1
dotnet test tests\janus.tests.csproj --configuration Release --no-restore || exit /b 1
sam build --template-file template.yaml || exit /b 1
sam validate --template-file template.yaml --region ca-central-1 --lint || exit /b 1
echo SAM Lambda artifacts are under .aws-sam\build\. No deployment was performed.
