---
name: rework-code
description: >
  Rework code based on PR review comments. Fetches comments via gh CLI, categorizes
  by layer, spawns backend + frontend fix agents in worktrees, runs QA, and pushes fixes.
  Usage: /rework-code {PR#}. Use this skill when the user wants to fix PR comments,
  address review feedback on a code PR, apply reviewer suggestions, or says things like
  "fix the PR comments", "address the review feedback", or "the PR needs changes".
user-invocable: true
---

# /rework-code — Code Rework Workflow

You are the lead orchestrator for a code rework pass. A feature PR has received review
comments from a human reviewer. Your job is to coordinate fixing those comments.

---

## Step 0 — Parse input and resolve paths

`$ARGUMENTS` is the PR number (e.g. `42`).

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
TASKS_PATH      = .claude/tasks/{FEATURE_NAME}/tasks.json
BACKEND_WT      = .worktrees/{FEATURE_NAME}/backend/
FRONTEND_WT     = .worktrees/{FEATURE_NAME}/frontend/
```

---

## Step 1 — Fetch PR review comments

First, resolve the repository owner and name:
```bash
REPO=$(gh repo view --json nameWithOwner --jq '.nameWithOwner')
```

Then fetch all comment types:
```bash
# Get review comments (inline code comments)
gh api "repos/${REPO}/pulls/{PR_NUMBER}/comments" --jq '.[] | "**" + .user.login + "** on `" + .path + "` line " + (.line|tostring) + ":\n" + .body + "\n"'

# Get review body comments
gh api "repos/${REPO}/pulls/{PR_NUMBER}/reviews" --jq '.[] | select(.body != "") | "**" + .user.login + "** (review):\n" + .body + "\n"'

# Get issue-style comments
gh api "repos/${REPO}/issues/{PR_NUMBER}/comments" --jq '.[] | "**" + .user.login + "**:\n" + .body + "\n"'
```

---

## Step 2 — Categorize comments

Categorize each comment as:
- **infra** — references AppHost, Program.cs service registration, Keycloak, RabbitMQ setup,
  SMTP config, seeding, appsettings, docker, Aspire
- **backend** — references `src/backend/`, mentions endpoints, entities, handlers, migrations, tests
- **frontend** — references `src/frontend/`, mentions components, stores, services, templates, e2e
- **both** — spans multiple layers

Build three lists:
- `INFRA_COMMENTS` — infra + relevant "both" comments
- `BACKEND_COMMENTS` — backend + relevant "both" comments
- `FRONTEND_COMMENTS` — frontend + relevant "both" comments

If any list is empty, skip that layer entirely.

---

## Step 3 — Set up worktrees

```bash
# Checkout feature branch
git checkout "feat/{FEATURE_NAME}"
git pull origin "feat/{FEATURE_NAME}"

# Set up worktrees (remove stale, create fresh)
[ -d ".worktrees/{FEATURE_NAME}/backend" ] \
  && (git worktree remove --force ".worktrees/{FEATURE_NAME}/backend" 2>/dev/null || rm -rf ".worktrees/{FEATURE_NAME}/backend") \
  || true
[ -d ".worktrees/{FEATURE_NAME}/frontend" ] \
  && (git worktree remove --force ".worktrees/{FEATURE_NAME}/frontend" 2>/dev/null || rm -rf ".worktrees/{FEATURE_NAME}/frontend") \
  || true

mkdir -p ".worktrees/{FEATURE_NAME}"
git rev-parse --verify "impl/{FEATURE_NAME}-backend" >/dev/null 2>&1 \
  && git worktree add ".worktrees/{FEATURE_NAME}/backend" "impl/{FEATURE_NAME}-backend" \
  || git worktree add -b "impl/{FEATURE_NAME}-backend" ".worktrees/{FEATURE_NAME}/backend" "feat/{FEATURE_NAME}"

git rev-parse --verify "impl/{FEATURE_NAME}-frontend" >/dev/null 2>&1 \
  && git worktree add ".worktrees/{FEATURE_NAME}/frontend" "impl/{FEATURE_NAME}-frontend" \
  || git worktree add -b "impl/{FEATURE_NAME}-frontend" ".worktrees/{FEATURE_NAME}/frontend" "feat/{FEATURE_NAME}"

# Sync worktrees with feature branch (impl branches may be behind feat/ from previous reworks)
cd ".worktrees/{FEATURE_NAME}/backend" && git merge feat/{FEATURE_NAME} --no-edit && cd -
cd ".worktrees/{FEATURE_NAME}/frontend" && git merge feat/{FEATURE_NAME} --no-edit && cd -
```

---

## Step 4 — Fix pass (parallel if both layers need work)

Spawn fix agents **in the same response** (all that have comments):

**Infra fix agent** (if `INFRA_COMMENTS` not empty):
1. Read `.claude/skills/rework-code/phase-fix-backend.md`
2. Spawn **infra-impl** agent with that content, appending:
```
--- CONTEXT ---
SPEC_PATH:        {SPEC_PATH}
TASKS_PATH:       {TASKS_PATH}
BACKEND_WT:       {BACKEND_WT}
FEATURE_BRANCH:   {FEATURE_BRANCH}

--- FIX PASS ---
{INFRA_COMMENTS}
```

**Backend fix agent** (if `BACKEND_COMMENTS` not empty):
1. Read `.claude/skills/rework-code/phase-fix-backend.md`
2. Spawn **backend-impl** agent with that content, appending:
```
--- CONTEXT ---
SPEC_PATH:        {SPEC_PATH}
TASKS_PATH:       {TASKS_PATH}
BACKEND_WT:       {BACKEND_WT}
FEATURE_BRANCH:   {FEATURE_BRANCH}

--- FIX PASS ---
{BACKEND_COMMENTS}
```

**Frontend fix agent** (if `FRONTEND_COMMENTS` not empty):
1. Read `.claude/skills/rework-code/phase-fix-frontend.md`
2. Spawn **frontend-impl** agent with that content, appending:
```
--- CONTEXT ---
SPEC_PATH:        {SPEC_PATH}
TASKS_PATH:       {TASKS_PATH}
FRONTEND_WT:      {FRONTEND_WT}
FEATURE_BRANCH:   {FEATURE_BRANCH}

--- FIX PASS ---
{FRONTEND_COMMENTS}
```

Wait for both to complete.

---

## Step 5 — Quality checks (parallel, only for changed layers)

Only run QA for layers that had comments and received fixes. Skip QA for layers with no changes.

Spawn QA agents **in the same response** (only those that had fixes):

**Backend QA** (if `BACKEND_COMMENTS` or `INFRA_COMMENTS` was not empty): Spawn `chairly-backend-qa`, append `BACKEND_WT: {BACKEND_WT}`
**Frontend QA** (if `FRONTEND_COMMENTS` was not empty): Spawn `chairly-frontend-qa`, append `FRONTEND_WT: {FRONTEND_WT}`

If QA fails, spawn fix agents with QA notes, retry up to 2 times.

---

## Step 6 — Commit and push (only changed layers)

Only commit/push/merge layers that had comments and received fixes.

```bash
# Backend (if BACKEND_COMMENTS or INFRA_COMMENTS was not empty)
cd {BACKEND_WT} && git add -A
git diff --cached --quiet || git commit -m "fix({FEATURE_NAME}): address PR review comments (backend)"
git push origin impl/{FEATURE_NAME}-backend

# Frontend (if FRONTEND_COMMENTS was not empty)
cd {FRONTEND_WT} && git add -A
git diff --cached --quiet || git commit -m "fix({FEATURE_NAME}): address PR review comments (frontend)"
git push origin impl/{FEATURE_NAME}-frontend

# Merge into feature branch (only merge branches that had changes)
git checkout feat/{FEATURE_NAME}
# Only merge backend if it had fixes:
git merge --no-ff impl/{FEATURE_NAME}-backend -m "chore: merge backend rework fixes"
# Only merge frontend if it had fixes:
git merge --no-ff impl/{FEATURE_NAME}-frontend -m "chore: merge frontend rework fixes"
git push origin feat/{FEATURE_NAME}
```

---

## Step 7 — Reply to PR

```bash
gh pr comment {PR_NUMBER} --body "$(cat <<'EOF'
## Rework complete

Addressed all review comments. Changes pushed to `feat/{FEATURE_NAME}`.

**Backend fixes:**
{bullet list or "No backend changes needed"}

**Frontend fixes:**
{bullet list or "No frontend changes needed"}

**Quality gates:** backend QA, frontend QA

Generated by the /rework-code workflow.
EOF
)"
```

---

## Step 8 — Return to main and report

```bash
git checkout main
```

Report PR URL and summary.

---

## Global rules

- Never modify `main` branch — all changes stay on the feature branch
- Only address comments from the PR — do not gold-plate or refactor beyond the comments
- If a comment is ambiguous, implement the most conservative interpretation
- Keep a running status log
