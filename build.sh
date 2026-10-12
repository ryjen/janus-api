#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")"

command -v dotnet >/dev/null || { echo 'The .NET 10 SDK is required' >&2; exit 1; }
command -v sam >/dev/null || { echo 'The AWS SAM CLI is required' >&2; exit 1; }
command -v zip >/dev/null || { echo 'The zip utility is required for the AWS Lambda .NET packager' >&2; exit 1; }

dotnet restore janus.sln
dotnet build janus.sln --configuration Release --no-restore
dotnet test tests/janus.tests.csproj --configuration Release --no-restore

# The SAM builder packages each project's source as a Lambda zip. It does not
# consume stale pre-published dist/ directories.
sam build --template-file template.yaml
sam validate --template-file template.yaml --region ca-central-1 --lint

echo 'SAM Lambda artifacts are under .aws-sam/build/. No deployment was performed.'
