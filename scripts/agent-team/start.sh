#!/usr/bin/env bash
# scripts/agent-team/start.sh
# Bootstrap the feature-team agent workflow from a feature description or brief.
#
# Usage:
#   ./scripts/agent-team/start.sh "Add booking cancellation"
#   ./scripts/agent-team/start.sh docs/briefs/my-feature.md
#
# Prerequisites: git, tmux, claude

set -euo pipefail

if [[ $# -lt 1 ]]; then
  echo "Usage: $0 <description or path/to/brief.md>" >&2
  exit 1
fi

FEATURE_INPUT="$*"

REPO_ROOT="$(git rev-parse --show-toplevel)"
cd "$REPO_ROOT"

for cmd in tmux claude; do
  if ! command -v "$cmd" >/dev/null 2>&1; then
    echo "Error: '$cmd' is not installed." >&2
    exit 1
  fi
done

TMUX_SESSION="feature-team"

if tmux has-session -t "$TMUX_SESSION" 2>/dev/null; then
  echo "Error: tmux session '$TMUX_SESSION' already exists." >&2
  echo "Kill it with: tmux kill-session -t $TMUX_SESSION" >&2
  exit 1
fi

tmux new-session -d -s "$TMUX_SESSION" -c "$REPO_ROOT"
tmux send-keys -t "$TMUX_SESSION" \
  "CLAUDE_CODE_EXPERIMENTAL_AGENT_TEAMS=1 claude --dangerously-skip-permissions" Enter

sleep 6

tmux send-keys -t "$TMUX_SESSION" "/feature-team ${FEATURE_INPUT}" Enter

echo ""
echo "Feature team started."
echo "Attach to watch progress:"
echo "  tmux attach -t $TMUX_SESSION"
echo ""
echo "Kill if needed:"
echo "  tmux kill-session -t $TMUX_SESSION"
