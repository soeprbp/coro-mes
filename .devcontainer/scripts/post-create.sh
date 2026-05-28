#!/usr/bin/env bash
set -euo pipefail

cd /workspaces/CoroMES

echo "Restoring CoroMES .NET packages..."
dotnet restore CoroMES.sln

echo
echo "Codespace is ready. Start the API with:"
echo "  dotnet run --project src/CoroMES.Api"
