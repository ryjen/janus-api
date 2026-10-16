#!/usr/bin/env bash
# Source-only qualification. This script NEVER calls sam deploy, CloudFormation
# mutation, or any AWS credentialed release operation.
set -euo pipefail
cd "$(dirname "$0")/.."

required=(dotnet sam zip cfn-lint python3)
for tool in "${required[@]}"; do
  if ! command -v "$tool" >/dev/null 2>&1; then
    printf 'Missing flake-provided build dependency: %s\n' "$tool" >&2
    exit 1
  fi
done

export SAM_CLI_TELEMETRY=0
export DOTNET_CLI_TELEMETRY_OPTOUT=1

./build.sh
cfn-lint template.yaml
python3 -m unittest discover -s tests -p 'test_template.py'
