@echo off
setlocal
dotnet restore janus.sln || exit /b 1
dotnet build janus.sln --configuration Release --no-restore || exit /b 1
dotnet test tests\janus.tests.csproj --configuration Release || exit /b 1
for %%D in (auth account player) do (
  dotnet publish %%D\janus.csproj --framework net10.0 --configuration Release --no-restore --output dist\%%D --self-contained false || exit /b 1
)
echo Lambda publish directories are in dist\. Run sam validate --lint separately.
