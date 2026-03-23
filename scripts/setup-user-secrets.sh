#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
ENV_FILE="$REPO_ROOT/.env"
PROJECT="src/backend/Chairly.AppHost/Chairly.AppHost.csproj"

if [ ! -f "$ENV_FILE" ]; then
  echo "Error: .env file not found at $ENV_FILE"
  echo "Copy .env.example to .env and fill in the secret values:"
  echo "  cp .env.example .env"
  exit 1
fi

# Parse .env file: skip comments and blank lines, read KEY=VALUE pairs
set -a
while IFS='=' read -r key value; do
  # Skip comments and empty lines
  [[ -z "$key" || "$key" =~ ^# ]] && continue
  # Trim whitespace
  key="$(echo "$key" | xargs)"
  value="$(echo "$value" | xargs)"
  # Skip placeholder values
  if [[ "$value" == "<"*">" ]]; then
    echo "WARNING: Skipping $key — still has placeholder value. Update your .env file."
    continue
  fi
  # Convert env var format (double underscore) to config format (colon)
  config_key="${key//__/:}"
  dotnet user-secrets set "$config_key" "$value" --project "$PROJECT"
done < "$ENV_FILE"
set +a

echo ""
echo "Done. All user-secrets configured for local development."
echo "Run the AppHost with: dotnet run --project $PROJECT"
