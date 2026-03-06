#!/usr/bin/env bash
# scripts/agent-team/start.sh
# Bootstrap the feature-team agent workflow.
#
# Usage:
#   ./scripts/agent-team/start.sh "Add booking CRUD for clients"
#   ./scripts/agent-team/start.sh /path/to/feature-description.md
#
# Prerequisites: git, gh (authenticated), tmux, claude (Claude Code CLI)

set -euo pipefail

# ---------------------------------------------------------------------------
# Validate input
# ---------------------------------------------------------------------------
if [[ $# -eq 0 ]]; then
  echo "Usage: $0 \"Feature description\" | /path/to/description.md" >&2
  exit 1
fi

FEATURE_ARG="$*"

# ---------------------------------------------------------------------------
# Resolve repo root (works from any subdirectory)
# ---------------------------------------------------------------------------
REPO_ROOT="$(git rev-parse --show-toplevel)"
cd "$REPO_ROOT"

# ---------------------------------------------------------------------------
# If input is a file path:
#   - derive feature name from the first H1 heading (strip "Feature: " prefix)
#     or fall back to the filename without extension
#   - pass the file PATH to the skill (SKILL.md reads the file itself)
# If input is free-form text:
#   - derive feature name from the text
#   - pass the text directly to the skill
# ---------------------------------------------------------------------------
if [[ -f "$FEATURE_ARG" ]]; then
  H1=$(grep -m1 '^# ' "$FEATURE_ARG" 2>/dev/null | sed 's/^# //;s/^Feature: //' || true)
  FEATURE_NAME_SOURCE="${H1:-$(basename "$FEATURE_ARG" .md)}"
  SKILL_INPUT="$FEATURE_ARG"
else
  FEATURE_NAME_SOURCE="$FEATURE_ARG"
  SKILL_INPUT="$FEATURE_ARG"
fi

# ---------------------------------------------------------------------------
# Derive kebab-case feature name (must match SKILL.md algorithm)
# lowercase → strip non-alphanumeric-or-space → spaces to hyphens → max 40 chars
# ---------------------------------------------------------------------------
FEATURE_NAME=$(echo "$FEATURE_NAME_SOURCE" \
  | tr '[:upper:]' '[:lower:]' \
  | sed 's/[^a-z0-9 ]//g' \
  | tr -s ' ' '-' \
  | sed 's/^-//;s/-$//' \
  | cut -c1-40)

FEATURE_BRANCH="feat/${FEATURE_NAME}"
BACKEND_BRANCH="${FEATURE_BRANCH}/backend"
FRONTEND_BRANCH="${FEATURE_BRANCH}/frontend"
BACKEND_WT="${REPO_ROOT}/.worktrees/backend"
FRONTEND_WT="${REPO_ROOT}/.worktrees/frontend"
TMUX_SESSION="feature-team-${FEATURE_NAME}"

echo "Feature name:   $FEATURE_NAME"
echo "Feature branch: $FEATURE_BRANCH"

# ---------------------------------------------------------------------------
# Check for tmux
# ---------------------------------------------------------------------------
if ! command -v tmux >/dev/null 2>&1; then
  echo "Error: tmux is not installed. Install with: sudo apt-get install tmux" >&2
  exit 1
fi

# ---------------------------------------------------------------------------
# Check for an existing tmux session with the same name
# ---------------------------------------------------------------------------
if tmux has-session -t "$TMUX_SESSION" 2>/dev/null; then
  echo "Error: tmux session '$TMUX_SESSION' already exists." >&2
  echo "Kill it with: tmux kill-session -t $TMUX_SESSION" >&2
  exit 1
fi

# ---------------------------------------------------------------------------
# Ensure main is up to date
# ---------------------------------------------------------------------------
echo "Fetching latest main..."
git fetch origin main
git checkout main
git pull --ff-only origin main

# ---------------------------------------------------------------------------
# Create feature branch from main
# ---------------------------------------------------------------------------
if git rev-parse --verify "$FEATURE_BRANCH" >/dev/null 2>&1; then
  echo "Branch $FEATURE_BRANCH already exists, reusing it."
  git checkout "$FEATURE_BRANCH"
else
  git checkout -b "$FEATURE_BRANCH"
fi

# ---------------------------------------------------------------------------
# Create git worktrees
# ---------------------------------------------------------------------------
# Remove stale worktrees if they exist (e.g. from a previous failed run)
if [[ -d "$BACKEND_WT" ]]; then
  echo "Removing stale backend worktree..."
  git worktree remove --force "$BACKEND_WT" 2>/dev/null || rm -rf "$BACKEND_WT"
fi
if [[ -d "$FRONTEND_WT" ]]; then
  echo "Removing stale frontend worktree..."
  git worktree remove --force "$FRONTEND_WT" 2>/dev/null || rm -rf "$FRONTEND_WT"
fi

# Create backend worktree on its own branch
if git rev-parse --verify "$BACKEND_BRANCH" >/dev/null 2>&1; then
  git worktree add "$BACKEND_WT" "$BACKEND_BRANCH"
else
  git worktree add -b "$BACKEND_BRANCH" "$BACKEND_WT" "$FEATURE_BRANCH"
fi

# Create frontend worktree on its own branch
if git rev-parse --verify "$FRONTEND_BRANCH" >/dev/null 2>&1; then
  git worktree add "$FRONTEND_WT" "$FRONTEND_BRANCH"
else
  git worktree add -b "$FRONTEND_BRANCH" "$FRONTEND_WT" "$FEATURE_BRANCH"
fi

echo "Worktrees created:"
echo "  Backend:  $BACKEND_WT  ($BACKEND_BRANCH)"
echo "  Frontend: $FRONTEND_WT  ($FRONTEND_BRANCH)"

# ---------------------------------------------------------------------------
# Create .claude/tasks directory for this feature
# ---------------------------------------------------------------------------
mkdir -p "${REPO_ROOT}/.claude/tasks/${FEATURE_NAME}"

# ---------------------------------------------------------------------------
# Start tmux session and launch Claude Code
# ---------------------------------------------------------------------------
echo ""
echo "Starting tmux session: $TMUX_SESSION"

tmux new-session -d -s "$TMUX_SESSION" -c "$REPO_ROOT"

# Launch Claude with all permissions (autonomous mode)
tmux send-keys -t "$TMUX_SESSION" \
  "claude --dangerously-skip-permissions" Enter

# Wait for Claude to start and display the prompt
sleep 6

# Invoke the feature-team skill
# Pass the file path (or free-form text) — escape single quotes for shell safety
ESCAPED_INPUT=$(printf '%s' "$SKILL_INPUT" | sed "s/'/'\\\\''/g")
tmux send-keys -t "$TMUX_SESSION" "/feature-team '${ESCAPED_INPUT}'" Enter

echo ""
echo "Feature team started."
echo "Attach to watch progress:"
echo "  tmux attach -t $TMUX_SESSION"
echo ""
echo "Kill if needed:"
echo "  tmux kill-session -t $TMUX_SESSION"
