#!/usr/bin/env bash
# scripts/agent-team/rework.sh
# Bootstrap the rework-team agent workflow from a PR number.
#
# Usage:
#   ./scripts/agent-team/rework.sh 42
#
# Prerequisites: git, gh (authenticated), tmux, claude, jq

set -euo pipefail

# ---------------------------------------------------------------------------
# Validate input
# ---------------------------------------------------------------------------
if [[ $# -ne 1 ]] || ! [[ "$1" =~ ^[0-9]+$ ]]; then
  echo "Usage: $0 <pr-number>" >&2
  exit 1
fi

PR_NUMBER="$1"

# ---------------------------------------------------------------------------
# Resolve repo root
# ---------------------------------------------------------------------------
REPO_ROOT="$(git rev-parse --show-toplevel)"
cd "$REPO_ROOT"

# ---------------------------------------------------------------------------
# Check dependencies
# ---------------------------------------------------------------------------
for cmd in gh jq tmux; do
  if ! command -v "$cmd" >/dev/null 2>&1; then
    echo "Error: '$cmd' is not installed." >&2
    exit 1
  fi
done

# ---------------------------------------------------------------------------
# Resolve PR details
# ---------------------------------------------------------------------------
echo "Fetching PR #${PR_NUMBER} details..."

PR_JSON=$(gh pr view "$PR_NUMBER" --json headRefName,title,body)
FEATURE_BRANCH=$(echo "$PR_JSON" | jq -r '.headRefName')
PR_TITLE=$(echo "$PR_JSON" | jq -r '.title')

if [[ "$FEATURE_BRANCH" != feat/* ]]; then
  echo "Error: PR head branch '$FEATURE_BRANCH' does not start with 'feat/'." >&2
  echo "This script only handles feature-team PRs." >&2
  exit 1
fi

# Strip "feat/" prefix to get feature name
FEATURE_NAME="${FEATURE_BRANCH#feat/}"
# Remove any trailing /backend or /frontend if present
FEATURE_NAME="${FEATURE_NAME%/backend}"
FEATURE_NAME="${FEATURE_NAME%/frontend}"

BACKEND_BRANCH="impl/${FEATURE_NAME}-backend"
FRONTEND_BRANCH="impl/${FEATURE_NAME}-frontend"
BACKEND_WT="${REPO_ROOT}/.worktrees/backend"
FRONTEND_WT="${REPO_ROOT}/.worktrees/frontend"
TASKS_DIR="${REPO_ROOT}/.claude/tasks/${FEATURE_NAME}"
COMMENTS_PATH="${TASKS_DIR}/pr-comments.md"
TMUX_SESSION="rework-team-${FEATURE_NAME}"

echo "Feature name:   $FEATURE_NAME"
echo "Feature branch: $FEATURE_BRANCH"
echo "PR title:       $PR_TITLE"

# ---------------------------------------------------------------------------
# Check for existing tmux session
# ---------------------------------------------------------------------------
if tmux has-session -t "$TMUX_SESSION" 2>/dev/null; then
  echo "Error: tmux session '$TMUX_SESSION' already exists." >&2
  echo "Kill it with: tmux kill-session -t $TMUX_SESSION" >&2
  exit 1
fi

# ---------------------------------------------------------------------------
# Fetch and format PR review comments
# ---------------------------------------------------------------------------
mkdir -p "$TASKS_DIR"

REPO_NAME=$(gh repo view --json nameWithOwner --jq .nameWithOwner)

echo "Fetching PR review comments..."

# Inline diff comments (code-level)
INLINE_COMMENTS=$(gh api \
  "repos/${REPO_NAME}/pulls/${PR_NUMBER}/comments" \
  --jq '.[] | "### Comment on \(.path) (line \(.line // "N/A"))\n\n\(.body)\n"' \
  2>/dev/null || echo "")

# PR-level review comments (review bodies)
REVIEW_COMMENTS=$(gh api \
  "repos/${REPO_NAME}/pulls/${PR_NUMBER}/reviews" \
  --jq '.[] | select(.body != "") | "### Review by \(.user.login) (\(.state))\n\n\(.body)\n"' \
  2>/dev/null || echo "")

# General issue comments
ISSUE_COMMENTS=$(gh api \
  "repos/${REPO_NAME}/issues/${PR_NUMBER}/comments" \
  --jq '.[] | "### Comment by \(.user.login)\n\n\(.body)\n"' \
  2>/dev/null || echo "")

cat > "$COMMENTS_PATH" <<EOF
# PR #${PR_NUMBER} Review Comments
# Branch: ${FEATURE_BRANCH}
# Title: ${PR_TITLE}
# Fetched: $(date -u +"%Y-%m-%dT%H:%M:%SZ")

---

## Inline Code Comments

${INLINE_COMMENTS:-_No inline comments._}

---

## Review Summaries

${REVIEW_COMMENTS:-_No review summaries._}

---

## General Comments

${ISSUE_COMMENTS:-_No general comments._}
EOF

echo "PR comments written to: $COMMENTS_PATH"

COMMENT_COUNT=$(echo "$INLINE_COMMENTS$REVIEW_COMMENTS$ISSUE_COMMENTS" \
  | grep -c '^### ' 2>/dev/null || echo "0")
echo "Total comments found: $COMMENT_COUNT"

if [[ "$COMMENT_COUNT" -eq 0 ]]; then
  echo "Warning: no review comments found on PR #${PR_NUMBER}." >&2
  echo "Proceeding anyway — the rework agent will report nothing to fix." >&2
fi

# ---------------------------------------------------------------------------
# Checkout the feature branch
# ---------------------------------------------------------------------------
echo "Checking out $FEATURE_BRANCH..."
git fetch origin "$FEATURE_BRANCH"
git checkout "$FEATURE_BRANCH"
git pull --ff-only origin "$FEATURE_BRANCH" 2>/dev/null || true

# ---------------------------------------------------------------------------
# Re-create worktrees from the feature branch
# ---------------------------------------------------------------------------
if [[ -d "$BACKEND_WT" ]]; then
  echo "Removing stale backend worktree..."
  git worktree remove --force "$BACKEND_WT" 2>/dev/null || rm -rf "$BACKEND_WT"
fi
if [[ -d "$FRONTEND_WT" ]]; then
  echo "Removing stale frontend worktree..."
  git worktree remove --force "$FRONTEND_WT" 2>/dev/null || rm -rf "$FRONTEND_WT"
fi

# Fetch worktree branches (they were pushed during the original workflow)
git fetch origin "$BACKEND_BRANCH" 2>/dev/null || true
git fetch origin "$FRONTEND_BRANCH" 2>/dev/null || true

if git rev-parse --verify "origin/${BACKEND_BRANCH}" >/dev/null 2>&1; then
  git branch --force "$BACKEND_BRANCH" "origin/${BACKEND_BRANCH}" 2>/dev/null || true
  git worktree add "$BACKEND_WT" "$BACKEND_BRANCH"
else
  echo "Backend branch $BACKEND_BRANCH not found, creating from $FEATURE_BRANCH"
  git worktree add -b "$BACKEND_BRANCH" "$BACKEND_WT" "$FEATURE_BRANCH"
fi

if git rev-parse --verify "origin/${FRONTEND_BRANCH}" >/dev/null 2>&1; then
  git branch --force "$FRONTEND_BRANCH" "origin/${FRONTEND_BRANCH}" 2>/dev/null || true
  git worktree add "$FRONTEND_WT" "$FRONTEND_BRANCH"
else
  echo "Frontend branch $FRONTEND_BRANCH not found, creating from $FEATURE_BRANCH"
  git worktree add -b "$FRONTEND_BRANCH" "$FRONTEND_WT" "$FEATURE_BRANCH"
fi

echo "Worktrees created:"
echo "  Backend:  $BACKEND_WT  ($BACKEND_BRANCH)"
echo "  Frontend: $FRONTEND_WT  ($FRONTEND_BRANCH)"

# ---------------------------------------------------------------------------
# Start tmux session and launch Claude Code
# ---------------------------------------------------------------------------
echo ""
echo "Starting tmux session: $TMUX_SESSION"

tmux new-session -d -s "$TMUX_SESSION" -c "$REPO_ROOT"
tmux send-keys -t "$TMUX_SESSION" \
  "claude --dangerously-skip-permissions" Enter

sleep 6

tmux send-keys -t "$TMUX_SESSION" "/rework-team ${PR_NUMBER}" Enter

echo ""
echo "Rework team started."
echo "Attach to watch progress:"
echo "  tmux attach -t $TMUX_SESSION"
echo ""
echo "Kill if needed:"
echo "  tmux kill-session -t $TMUX_SESSION"
