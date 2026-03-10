#!/usr/bin/env bash
# scripts/agent-team/hooks/task-completed.sh
# Claude Code TaskCompleted hook — runs a quick build check when an agent
# marks a task complete. Exit code 2 blocks the completion with feedback.
#
# Event data is received as JSON on stdin.
# Expected fields: { "task_id": "B1" | "F1" | ... }
#
# NOTE: Verify the exact JSON path for task_id against your Claude Code version.
# If the hook silently does nothing, add `echo "$EVENT" >> /tmp/hook-debug.log`
# temporarily to inspect the raw event structure.

set -euo pipefail

REPO_ROOT="$(git rev-parse --show-toplevel)"

# ---------------------------------------------------------------------------
# Guard: require jq
# ---------------------------------------------------------------------------
if ! command -v jq >/dev/null 2>&1; then
  exit 0
fi

# ---------------------------------------------------------------------------
# Read event data from stdin
# ---------------------------------------------------------------------------
EVENT=$(cat)

if [[ -z "$EVENT" ]]; then
  exit 0
fi

# ---------------------------------------------------------------------------
# Extract task ID
# Try the most likely path first; adjust if Claude Code changes its schema.
# ---------------------------------------------------------------------------
TASK_ID=$(echo "$EVENT" | jq -r '.task_id // .id // .taskId // empty' 2>/dev/null || true)

if [[ -z "$TASK_ID" || "$TASK_ID" == "null" ]]; then
  # Cannot determine task ID — let completion proceed
  exit 0
fi

# ---------------------------------------------------------------------------
# Determine layer from task ID prefix
# ---------------------------------------------------------------------------
LAYER=""
case "$TASK_ID" in
  B*) LAYER="backend" ;;
  F*) LAYER="frontend" ;;
  *)  exit 0 ;;  # Unknown prefix — not our task
esac

# ---------------------------------------------------------------------------
# Run quick build check in the appropriate worktree
# ---------------------------------------------------------------------------
if [[ "$LAYER" == "backend" ]]; then
  # Find the first active backend worktree under .worktrees/*/backend
  WORKTREE=$(find "${REPO_ROOT}/.worktrees" -maxdepth 2 -name "backend" -type d 2>/dev/null | head -1)

  if [[ -z "$WORKTREE" || ! -d "$WORKTREE" ]]; then
    exit 0
  fi

  cd "$WORKTREE"
  RESULT=$(dotnet build src/backend/Chairly.slnx \
    --nologo --verbosity minimal 2>&1) || BUILD_FAILED=1

  if [[ "${BUILD_FAILED:-0}" -eq 1 ]]; then
    # Exit code 2 sends feedback to the agent and blocks task completion
    echo "Backend build failed after completing task ${TASK_ID}. Fix the errors before marking done."
    echo ""
    echo "$RESULT" | tail -30
    exit 2
  fi

elif [[ "$LAYER" == "frontend" ]]; then
  # Find the first active frontend worktree under .worktrees/*/frontend
  WORKTREE=$(find "${REPO_ROOT}/.worktrees" -maxdepth 2 -name "frontend" -type d 2>/dev/null | head -1)

  if [[ -z "$WORKTREE" || ! -d "$WORKTREE" ]]; then
    exit 0
  fi

  cd "${WORKTREE}/src/frontend/chairly"
  RESULT=$(npx nx affected -t build --base=main 2>&1) || BUILD_FAILED=1

  if [[ "${BUILD_FAILED:-0}" -eq 1 ]]; then
    echo "Frontend build failed after completing task ${TASK_ID}. Fix the errors before marking done."
    echo ""
    echo "$RESULT" | tail -30
    exit 2
  fi
fi

exit 0
