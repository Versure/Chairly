---
name: fix
description: >
  Quick bug fix workflow — skips the spec PR cycle. Describes the bug, creates a fix branch,
  spawns fix agents, runs QA, and creates a PR in one pass. Usage: /fix {description}.
  Use this skill when the user reports a bug, wants a quick fix, needs a hotfix, or says
  things like "fix this bug", "this is broken", "there's an issue with...", or "quick fix for...".
  Do not use for new features or large changes — use /create-spec + /implement instead.
user-invocable: true
---

# /fix — Quick Bug Fix Workflow

You are the lead orchestrator for a quick bug fix. Unlike the full `/create-spec` + `/implement`
pipeline, this workflow skips the spec PR review cycle. It's designed for small, well-understood
bugs where the overhead of a separate spec PR is not justified.

**When to use /fix vs /implement:**
- `/fix` — Small bugs (1-4 tasks), clear reproduction, no new entities or API contracts
- `/implement` — New features, large changes, anything that benefits from spec review

---

## Step 0 — Parse input and set up

### 0a — Parse input

`$ARGUMENTS` is a free-text description of the bug (e.g. "manager can't see revenue on dashboard").

Derive a kebab-case name from the description:
```
BUG_NAME        = {kebab-case, max 40 chars, e.g. "manager-revenue-dashboard"}
FIX_BRANCH      = fix/{BUG_NAME}
BACKEND_WT      = .worktrees/{BUG_NAME}/backend/
FRONTEND_WT     = .worktrees/{BUG_NAME}/frontend/
```

### 0b — Verify we are on main

```bash
git rev-parse --abbrev-ref HEAD
```

If not `main`, abort:
> Error: /fix must be run from the main branch.

### 0c — Investigate the bug

Before writing any code, investigate:

1. Search the codebase for relevant files based on the bug description
2. Read the relevant source files to understand the current behavior
3. Identify the root cause and determine which layers need changes

Set flags:
```
NEEDS_BACKEND  = true/false
NEEDS_FRONTEND = true/false
```

Report to the user:
> **Bug:** {description}
> **Root cause:** {brief explanation}
> **Fix plan:** {1-3 bullet points}
> **Layers:** {backend/frontend/both}

### 0d — Create branch and worktrees

```bash
# Create fix branch if it doesn't exist (idempotent)
git rev-parse --verify "fix/{BUG_NAME}" >/dev/null 2>&1 \
  || git branch "fix/{BUG_NAME}"

mkdir -p ".worktrees/{BUG_NAME}"
```

Only create worktrees for needed layers. Clean up stale ones first:

If `NEEDS_BACKEND`:
```bash
[ -d ".worktrees/{BUG_NAME}/backend" ] \
  && (git worktree remove --force ".worktrees/{BUG_NAME}/backend" 2>/dev/null || rm -rf ".worktrees/{BUG_NAME}/backend") \
  || true
git rev-parse --verify "impl/{BUG_NAME}-backend" >/dev/null 2>&1 \
  && git worktree add ".worktrees/{BUG_NAME}/backend" "impl/{BUG_NAME}-backend" \
  || git worktree add -b "impl/{BUG_NAME}-backend" ".worktrees/{BUG_NAME}/backend" "fix/{BUG_NAME}"
```

If `NEEDS_FRONTEND`:
```bash
[ -d ".worktrees/{BUG_NAME}/frontend" ] \
  && (git worktree remove --force ".worktrees/{BUG_NAME}/frontend" 2>/dev/null || rm -rf ".worktrees/{BUG_NAME}/frontend") \
  || true
git rev-parse --verify "impl/{BUG_NAME}-frontend" >/dev/null 2>&1 \
  && git worktree add ".worktrees/{BUG_NAME}/frontend" "impl/{BUG_NAME}-frontend" \
  || git worktree add -b "impl/{BUG_NAME}-frontend" ".worktrees/{BUG_NAME}/frontend" "fix/{BUG_NAME}"
```

---

## Step 1 — Fix (parallel if both layers)

Spawn fix agents **in the same response** for all needed layers:

**Backend fix** (if `NEEDS_BACKEND`):
1. Read `.claude/skills/implement/phase-backend.md`
2. Spawn **backend-impl** agent with that content, appending:
```
--- CONTEXT ---
SPEC_PATH:      SKIP (this is a bug fix — no spec exists, do NOT attempt to read a spec file)
TASKS_PATH:     SKIP (no tasks file — task is described inline below)
BACKEND_WT:     {BACKEND_WT}
Backend tasks:  B1 — {root cause description and fix instructions}

--- BUG FIX ---
Bug: {description}
Root cause: {explanation}
Fix: {what to change}

IMPORTANT: This is a bug fix, not a feature. Only change what is necessary to fix the bug.
Write tests that reproduce the bug and verify the fix.
```

**Frontend fix** (if `NEEDS_FRONTEND`):
1. Read `.claude/skills/implement/phase-frontend.md`
2. Spawn **frontend-impl** agent with that content, appending:
```
--- CONTEXT ---
SPEC_PATH:      SKIP (this is a bug fix — no spec exists, do NOT attempt to read a spec file)
TASKS_PATH:     SKIP (no tasks file — task is described inline below)
FRONTEND_WT:    {FRONTEND_WT}
Frontend tasks: F1 — {root cause description and fix instructions}

--- BUG FIX ---
Bug: {description}
Root cause: {explanation}
Fix: {what to change}

IMPORTANT: This is a bug fix, not a feature. Only change what is necessary to fix the bug.
Write tests that verify the fix.
```

Wait for all agents to complete.

---

## Step 2 — Quality checks (parallel, only active layers)

**Backend QA** (if `NEEDS_BACKEND`): Spawn `chairly-backend-qa`, append `BACKEND_WT: {BACKEND_WT}`
**Frontend QA** (if `NEEDS_FRONTEND`): Spawn `chairly-frontend-qa`, append `FRONTEND_WT: {FRONTEND_WT}`

If QA fails, spawn fix agents with QA notes, retry up to 2 times.

---

## Step 3 — Commit, merge, and create PR

```bash
# Commit and push each active worktree
# If NEEDS_BACKEND:
cd {BACKEND_WT} && git add -A
git diff --cached --quiet || git commit -m "fix({BUG_NAME}): {short fix description}"
git push -u origin impl/{BUG_NAME}-backend

# If NEEDS_FRONTEND:
cd {FRONTEND_WT} && git add -A
git diff --cached --quiet || git commit -m "fix({BUG_NAME}): {short fix description}"
git push -u origin impl/{BUG_NAME}-frontend

# Return to repo root before merging
cd <REPO_ROOT>
git checkout fix/{BUG_NAME}
# If NEEDS_BACKEND:
git merge --no-ff impl/{BUG_NAME}-backend -m "chore: merge backend fix"
# If NEEDS_FRONTEND:
git merge --no-ff impl/{BUG_NAME}-frontend -m "chore: merge frontend fix"

git push -u origin fix/{BUG_NAME}

# Create PR
gh pr create \
  --title "fix({BUG_NAME}): {short description}" \
  --body "$(cat <<'EOF'
## Bug Fix

**Bug:** {description}
**Root cause:** {explanation}

## Changes

{bullet list of changes per layer}

## Quality gates

{list gates that passed}

Quick fix via the /fix workflow.
EOF
)" \
  --base main \
  --head fix/{BUG_NAME}
```

---

## Step 4 — Clean up and report

```bash
git checkout main
```

Remove worktrees:
```bash
git worktree remove ".worktrees/{BUG_NAME}/backend" 2>/dev/null || true
git worktree remove ".worktrees/{BUG_NAME}/frontend" 2>/dev/null || true
```

Report the PR URL.

---

## Global rules

- Never gold-plate — only fix the bug, nothing more
- Always write a test that reproduces the bug before fixing it
- If the bug is too complex for a quick fix (needs new entities, API changes, etc.),
  recommend `/create-spec` + `/implement` instead
- Keep a running status log
