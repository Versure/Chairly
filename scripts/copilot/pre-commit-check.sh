#!/usr/bin/env bash
# Pre-commit quality gate for Copilot CLI hooks.
# This hook runs on preToolUse. It checks if the tool being used is a git commit,
# and if so, runs the appropriate quality checks based on changed files.
#
# Environment variables set by Copilot:
#   COPILOT_TOOL_NAME — the tool being invoked (e.g., "shell")
#   COPILOT_TOOL_INPUT — the tool input (e.g., the shell command)

set -euo pipefail

# Only gate git commit commands
if [[ "${COPILOT_TOOL_NAME:-}" != "shell" ]]; then
  exit 0
fi

if ! echo "${COPILOT_TOOL_INPUT:-}" | grep -q "git commit"; then
  exit 0
fi

echo "Pre-commit quality gate: checking changed files..."

# Determine what changed
BACKEND_CHANGED=false
FRONTEND_CHANGED=false

if git diff --cached --name-only | grep -q "^src/backend/"; then
  BACKEND_CHANGED=true
fi

if git diff --cached --name-only | grep -q "^src/frontend/"; then
  FRONTEND_CHANGED=true
fi

EXIT_CODE=0

if [[ "$BACKEND_CHANGED" == "true" ]]; then
  echo "Backend changes detected. Running quality checks..."
  if ! dotnet build src/backend/Chairly.slnx; then
    echo "FAIL: dotnet build failed"
    EXIT_CODE=1
  fi
  if ! dotnet test src/backend/Chairly.slnx; then
    echo "FAIL: dotnet test failed"
    EXIT_CODE=1
  fi
  if ! dotnet format src/backend/Chairly.slnx --verify-no-changes; then
    echo "FAIL: dotnet format check failed. Run 'dotnet format src/backend/Chairly.slnx' to fix."
    EXIT_CODE=1
  fi
fi

if [[ "$FRONTEND_CHANGED" == "true" ]]; then
  echo "Frontend changes detected. Running quality checks..."
  cd src/frontend/chairly
  if ! npx nx affected -t lint --base=main; then
    echo "FAIL: nx lint failed"
    EXIT_CODE=1
  fi
  if ! npx nx format:check --base=main; then
    echo "FAIL: nx format:check failed. Run 'npx nx format --base=main' to fix."
    EXIT_CODE=1
  fi
  if ! npx nx affected -t test --base=main; then
    echo "FAIL: nx test failed"
    EXIT_CODE=1
  fi
  if ! npx nx affected -t build --base=main; then
    echo "FAIL: nx build failed"
    EXIT_CODE=1
  fi
  cd ../../..
fi

if [[ "$EXIT_CODE" -ne 0 ]]; then
  echo "Quality gate FAILED. Fix the issues above before committing."
  exit 1
fi

echo "Quality gate passed."
exit 0
