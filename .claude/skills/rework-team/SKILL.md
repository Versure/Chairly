---
name: rework-team
description: >
  Orchestrate a rework pass based on PR review comments. Reads comments fetched by
  rework.sh, spawns backend and frontend fix agents in worktrees, runs QA, and pushes
  fixes back to the feature branch. Triggered via rework.sh, not directly by the user.
user-invocable: true
---

# Rework Team Workflow — Lead Orchestration

You are the lead orchestrator for a rework pass. A pull request has received review
comments from a human reviewer. Your job is to coordinate fixing those comments.

---

## Step 0 — Parse input and resolve paths

`$ARGUMENTS` is the PR number (e.g. `42`).

Run the following to resolve the feature branch and feature name:

```bash
gh pr view $ARGUMENTS --json headRefName,title --jq '{branch: .headRefName, title: .title}'
```

The `headRefName` will be `feat/{FEATURE_NAME}`. Derive:

```
PR_NUMBER       = $ARGUMENTS
FEATURE_BRANCH  = {headRefName from gh}
FEATURE_NAME    = {branch name with "feat/" prefix stripped}
SPEC_DIR        = .claude/tasks/{FEATURE_NAME}/
SPEC_PATH       = .claude/tasks/{FEATURE_NAME}/spec.md
COMMENTS_PATH   = .claude/tasks/{FEATURE_NAME}/pr-comments.md
BACKEND_WT      = .worktrees/backend/
FRONTEND_WT     = .worktrees/frontend/
```

Verify `COMMENTS_PATH` exists (written by rework.sh before Claude started).
If it does not exist, abort with a clear error message.

---

## Step 1 — Read and categorize PR comments

Read `COMMENTS_PATH`. The file contains formatted PR review comments.

Categorize each comment as:
- **backend** — references files under `src/backend/`, mentions endpoints, entities,
  handlers, migrations, or tests in `Chairly.Tests/`
- **frontend** — references files under `src/frontend/`, mentions components, stores,
  services, templates, or e2e tests
- **both** — if a comment spans both layers (e.g. "the API contract changed so update
  both the backend model and the frontend interface")

Build two lists:
- `BACKEND_COMMENTS` — all backend + "both" comments
- `FRONTEND_COMMENTS` — all frontend + "both" comments

If either list is empty, skip that layer's fix agent entirely.

---

## Step 2 — Fix pass (run both fix agents in parallel if both layers have comments)

Spawn fix agents **in the same response** when both layers need work (parallel worktrees):

**Backend fix agent:**
1. Read `.claude/skills/rework-team/phase-rework-backend.md`
2. Spawn subagent with that content, appending:
```
--- CONTEXT ---
SPEC_PATH:        {SPEC_PATH}
BACKEND_WT:       {BACKEND_WT}
FEATURE_BRANCH:   {FEATURE_BRANCH}
PR_COMMENTS:
{BACKEND_COMMENTS — paste the full text of each relevant comment}
```

**Frontend fix agent:**
1. Read `.claude/skills/rework-team/phase-rework-frontend.md`
2. Spawn subagent with that content, appending:
```
--- CONTEXT ---
SPEC_PATH:        {SPEC_PATH}
FRONTEND_WT:      {FRONTEND_WT}
FEATURE_BRANCH:   {FEATURE_BRANCH}
PR_COMMENTS:
{FRONTEND_COMMENTS — paste the full text of each relevant comment}
```

Wait for both to complete.

---

## Step 3 — Quality checks (parallel)

Same as Phase 4 in the main workflow. Spawn both QA agents in the same response:

**Backend QA:** Read `.claude/agents/chairly-backend-qa.md`, spawn subagent, append:
```
--- CONTEXT ---
BACKEND_WT: {BACKEND_WT}
```

**Frontend QA:** Read `.claude/agents/chairly-frontend-qa.md`, spawn subagent, append:
```
--- CONTEXT ---
FRONTEND_WT: {FRONTEND_WT}
```

Parse `BACKEND-QA-RESULT` and `FRONTEND-QA-RESULT`. Apply the same fix-and-retry
cycle as the main workflow (up to 2 retries per layer).

---

## Step 4 — Commit and push

```bash
# Commit and push backend worktree (its branch is impl/{FEATURE_NAME}-backend)
cd .worktrees/backend && git add -A
git diff --cached --quiet || git commit -m "fix({FEATURE_NAME}): address PR review comments (backend)"
git push origin impl/{FEATURE_NAME}-backend

# Commit and push frontend worktree (its branch is impl/{FEATURE_NAME}-frontend)
cd .worktrees/frontend && git add -A
git diff --cached --quiet || git commit -m "fix({FEATURE_NAME}): address PR review comments (frontend)"
git push origin impl/{FEATURE_NAME}-frontend

# Merge worktree branches into feature branch in main checkout
git checkout {FEATURE_BRANCH}
git merge --no-ff impl/{FEATURE_NAME}-backend -m "chore: merge backend rework fixes"
git merge --no-ff impl/{FEATURE_NAME}-frontend -m "chore: merge frontend rework fixes"
git push origin {FEATURE_BRANCH}
```

---

## Step 5 — Reply to PR

Post a comment on the PR summarizing what was done:

```bash
gh pr comment {PR_NUMBER} --body "$(cat <<'EOF'
## Rework complete

Addressed all review comments. Changes pushed to `{FEATURE_BRANCH}`.

**Backend fixes:**
{bullet list of backend comments addressed, or "No backend changes needed"}

**Frontend fixes:**
{bullet list of frontend comments addressed, or "No frontend changes needed"}

**Quality gates:** backend ✓  frontend ✓

{If any QA checks could not be resolved: "⚠️ Known issue: {description}"}
EOF
)"
```

---

## Global rules

- Never modify `main` branch — all changes stay on `{FEATURE_BRANCH}`
- Only address comments that are in `COMMENTS_PATH` — do not gold-plate or refactor beyond the comments
- If a comment is ambiguous, implement the most conservative interpretation
- Keep a running status log: `[Step 0 ✓] [Step 1 ✓] [Backend fix running...] ...`
