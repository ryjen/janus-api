#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")"

command -v dotnet >/dev/null || { echo "dotnet SDK 10 is required" >&2; exit 1; }

dotnet restore janus.sln
dotnet build janus.sln --configuration Release --no-restore
dotnet test tests/janus.tests.csproj --configuration Release

for func in auth account player; do
  dotnet publish "$func/janus.csproj" --framework net10.0 --configuration Release --no-restore \
    --output "dist/$func" --self-contained false
done

echo 'Lambda publish directories are in dist/. Run sam validate --lint separately.'
