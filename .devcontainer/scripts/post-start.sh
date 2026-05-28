#!/usr/bin/env bash
set -euo pipefail

echo "Waiting for CoroMES PostgreSQL service..."
until pg_isready -h postgres -p 5432 -U postgres -d coromes >/dev/null 2>&1; do
  sleep 1
done

echo "PostgreSQL is ready at postgres:5432."
echo "Run the API with: dotnet run --project src/CoroMES.Api"
