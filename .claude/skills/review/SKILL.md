---
name: review
description: >
  Standalone code review — detects changed layers on a branch, spawns appropriate reviewer
  agents, and reports findings. Works outside the /implement workflow.
  Usage: /review [branch-name]. Defaults to the current branch.
  Use this skill when the user wants a code review, wants to check their changes before
  creating a PR, or says things like "review my changes", "check this branch",
  "review before PR", or "run the reviewers".
user-invocable: true
---

# /review — Standalone Code Review

You coordinate a code review of changes on a branch by spawning the appropriate reviewer
agents based on which files changed. This works independently of the `/implement` workflow
and is useful for reviewing manual changes or ad-hoc work.

---

## Step 0 — Parse input and detect changes

### 0a — Determine the branch

`$ARGUMENTS` is an optional branch name. If empty, use the current branch:

```bash
REVIEW_BRANCH=$(git rev-parse --abbrev-ref HEAD)
```

If a branch name was provided, check it out:
```bash
git checkout {REVIEW_BRANCH}
```

**If `REVIEW_BRANCH` is `main`, abort immediately:**
> The current branch is `main`. Only feature/fix branches can be reviewed.
> Switch to the branch you want reviewed, or pass its name: `/review feat/my-feature`
Then stop.

### 0b — Detect changed files

Compare the branch against `main` to find all changed files:

```bash
git diff --name-only main...{REVIEW_BRANCH}
```

### 0c — Categorize changes by layer

Based on the changed file paths, determine which layers need review:

```
HAS_BACKEND_CHANGES  = any file matching src/backend/**
HAS_FRONTEND_CHANGES = any file matching src/frontend/**
HAS_INFRA_CHANGES    = any file matching src/backend/**/Program.cs, **/AppHost/**, **/appsettings*, docker-compose*
```

If no changes detected:
> No changes found on `{REVIEW_BRANCH}` compared to `main`. Nothing to review.
Then stop.

Report:
> **Branch:** {REVIEW_BRANCH}
> **Files changed:** {count}
> **Layers:** {backend/frontend/infra — list active ones}

### 0d — Find or infer spec

Check if this branch name maps to a spec:
- `feat/{name}` → check `.claude/tasks/{name}/spec.md`
- `fix/{name}` → no spec expected
- Other branch types (`chore/`, `docs/`, etc.) → no spec lookup attempted

If a spec exists, set `SPEC_PATH`. Otherwise, set `SPEC_PATH` to empty.

---

## Step 1 — Spawn reviewers (parallel for active layers)

Spawn reviewer agents **in the same response** for all layers with changes:

**Backend reviewer** (if `HAS_BACKEND_CHANGES` or `HAS_INFRA_CHANGES`):
1. Read `.claude/skills/implement/phase-backend-review.md`
2. Spawn **backend-reviewer** agent (`subagent_type: backend-reviewer`) with that content, appending:
```
--- CONTEXT ---
SPEC_PATH:    {SPEC_PATH or "No spec — this is a standalone review"}
BACKEND_WT:   . (current checkout, not a worktree)
```

**Infra reviewer** (if `HAS_INFRA_CHANGES`):
1. Read `.claude/skills/implement/phase-infra-review.md`
2. Spawn **infra-reviewer** agent (`subagent_type: infra-reviewer`) with that content, appending:
```
--- CONTEXT ---
SPEC_PATH:    {SPEC_PATH or "No spec — this is a standalone review"}
BACKEND_WT:   . (current checkout, not a worktree)
```

**Frontend reviewer** (if `HAS_FRONTEND_CHANGES`):
1. Read `.claude/skills/implement/phase-frontend-review.md`
2. Spawn **frontend-reviewer** agent (`subagent_type: frontend-reviewer`) with that content, appending:
```
--- CONTEXT ---
SPEC_PATH:    {SPEC_PATH or "No spec — this is a standalone review"}
FRONTEND_WT:  . (current checkout, not a worktree)
```

Wait for all reviewers to complete.

---

## Step 2 — Compile and present findings

Collect findings from all reviewers and present a unified report:

```
## Code Review: {REVIEW_BRANCH}

### Backend {pass/issues found}
{findings or "No issues found"}

### Infrastructure {pass/issues found}
{findings or "No issues found — or skipped (no infra changes)"}

### Frontend {pass/issues found}
{findings or "No issues found"}

### Summary
- Total issues: {count}
- Critical: {count}
- Suggestions: {count}
```

---

## Step 3 — Offer to fix

If issues were found, ask the user:
> Would you like me to fix these issues? I can spawn fix agents to address the findings.

If the user says yes:
1. Set up worktrees for the affected layers
2. Spawn fix agents with the review findings (same pattern as `/rework-code` Step 4)
3. Run QA on the fixed layers
4. Commit and push

If the user says no, just report and stop.

---

## Global rules

- This is a read-only review by default — never modify code without user consent
- Reuse the existing review phase files from `/implement` to maintain consistency
- If on `main`, abort — there's nothing to review
- Keep the report concise — group related findings
