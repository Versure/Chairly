---
name: feature-team
description: >
  Orchestrate a full feature implementation using the agent team workflow.
  Runs spec → backend → frontend → review → QA → merge/PR phases autonomously.
  Use when implementing a new feature end-to-end.
user-invocable: true
---

# Feature Team Workflow — Lead Orchestration

You are the **lead orchestrator**. You coordinate all phases by spawning subagents.
Never implement features yourself — your job is to orchestrate, verify, and hand off.

---

## Step 0 — Parse input, set paths, and set up git

### 0a — Parse input

`$ARGUMENTS` is either:
- A free-form feature description (e.g. "Add booking CRUD for clients")
- A file path to a description file — if the argument looks like a path, read the file

If `$ARGUMENTS` is a file path:
- Derive the feature name from the first `# ` heading in the file (strip a leading "Feature: " prefix if present), or fall back to the filename without extension.
- `FEATURE_DESC` = the full file contents

If `$ARGUMENTS` is free-form text:
- Derive the feature name from the text itself.
- `FEATURE_DESC` = `$ARGUMENTS`

Derive a **kebab-case feature name** (lowercase, spaces → hyphens, strip special chars, max 40 chars):
- "Add booking CRUD for clients" → `add-booking-crud`
- "Staff schedule management" → `staff-schedule-management`

Set these variables (used in every phase instruction below):

```
FEATURE_NAME    = {derived kebab-case name}
FEATURE_DESC    = {full feature description text}
SPEC_DIR        = .claude/tasks/{FEATURE_NAME}/
SPEC_PATH       = .claude/tasks/{FEATURE_NAME}/spec.md
TASKS_PATH      = .claude/tasks/{FEATURE_NAME}/tasks.json
FEATURE_BRANCH  = feat/{FEATURE_NAME}
BACKEND_BRANCH  = impl/{FEATURE_NAME}-backend
FRONTEND_BRANCH = impl/{FEATURE_NAME}-frontend
BACKEND_WT      = .worktrees/backend/
FRONTEND_WT     = .worktrees/frontend/
```

### 0b — Verify we are on main

Run:
```bash
git rev-parse --abbrev-ref HEAD
```

If the result is not `main`, abort with:
> Error: /feature-team must be run from the main branch. Run `git checkout main && git pull origin main` first.

### 0c — Set up git branches and worktrees

Run the following commands (using the Bash tool):

```bash
# Create feature branch if it doesn't exist yet
git rev-parse --verify "feat/{FEATURE_NAME}" >/dev/null 2>&1 \
  || git branch "feat/{FEATURE_NAME}"

# Create spec/tasks directory
mkdir -p ".claude/tasks/{FEATURE_NAME}"

# Remove stale worktrees if they exist
[ -d ".worktrees/backend" ] \
  && (git worktree remove --force ".worktrees/backend" 2>/dev/null || rm -rf ".worktrees/backend") \
  || true
[ -d ".worktrees/frontend" ] \
  && (git worktree remove --force ".worktrees/frontend" 2>/dev/null || rm -rf ".worktrees/frontend") \
  || true

# Create backend worktree
git rev-parse --verify "impl/{FEATURE_NAME}-backend" >/dev/null 2>&1 \
  && git worktree add ".worktrees/backend" "impl/{FEATURE_NAME}-backend" \
  || git worktree add -b "impl/{FEATURE_NAME}-backend" ".worktrees/backend" "feat/{FEATURE_NAME}"

# Create frontend worktree
git rev-parse --verify "impl/{FEATURE_NAME}-frontend" >/dev/null 2>&1 \
  && git worktree add ".worktrees/frontend" "impl/{FEATURE_NAME}-frontend" \
  || git worktree add -b "impl/{FEATURE_NAME}-frontend" ".worktrees/frontend" "feat/{FEATURE_NAME}"
```

Log: `[Step 0 ✓] Feature: {FEATURE_NAME} | Branch: feat/{FEATURE_NAME} | Worktrees ready`

---

## Phase 0 — Write spec

1. Read `.claude/skills/feature-team/phase-0-spec.md`
2. Spawn a subagent with that file's content as the prompt, appending this context block:

```
--- CONTEXT ---
FEATURE_DESC:  {FEATURE_DESC}
SPEC_DIR:      {SPEC_DIR}
SPEC_PATH:     {SPEC_PATH}
TASKS_PATH:    {TASKS_PATH}
```

3. Wait for the subagent to finish.
4. Verify `SPEC_PATH` and `TASKS_PATH` both exist. If either is missing, re-spawn the spec
   subagent once more. Abort with an error message if still missing after retry.

---

## Phase 1 — Backend implementation

1. Read `TASKS_PATH`. Extract all tasks where `"layer": "backend"`.
2. Read `.claude/skills/feature-team/phase-1-backend.md`
3. Spawn a subagent with that file's content as the prompt, appending:

```
--- CONTEXT ---
SPEC_PATH:      {SPEC_PATH}
TASKS_PATH:     {TASKS_PATH}
BACKEND_WT:     {BACKEND_WT}
Backend tasks:  {list each backend task as "B1 — {title}"}
```

4. Wait for the subagent to finish.

---

## Phase 2 — Frontend implementation

1. Read `TASKS_PATH`. Extract all tasks where `"layer": "frontend"`.
2. Read `.claude/skills/feature-team/phase-2-frontend.md`
3. Spawn a subagent with that file's content as the prompt, appending:

```
--- CONTEXT ---
SPEC_PATH:      {SPEC_PATH}
TASKS_PATH:     {TASKS_PATH}
FRONTEND_WT:    {FRONTEND_WT}
Frontend tasks: {list each frontend task as "F1 — {title}"}
```

4. Wait for the subagent to finish.

---

## Phase 3 — Code review (run both reviewers in parallel)

Spawn the backend and frontend review subagents **in the same response** so they run in parallel.

**Backend reviewer:**
1. Read `.claude/skills/feature-team/phase-3-backend-review.md`
2. Spawn subagent with that content, appending:
```
--- CONTEXT ---
SPEC_PATH:    {SPEC_PATH}
BACKEND_WT:   {BACKEND_WT}
```

**Frontend reviewer:**
1. Read `.claude/skills/feature-team/phase-3-frontend-review.md`
2. Spawn subagent with that content, appending:
```
--- CONTEXT ---
SPEC_PATH:    {SPEC_PATH}
FRONTEND_WT:  {FRONTEND_WT}
```

Wait for both to return their findings.

### Fix cycle (if findings exist)

If a reviewer returned issues, spawn fix subagents (parallel if both layers need fixes):

- **Backend fix**: use the same prompt as Phase 1 + append the backend reviewer's findings
  under a `--- FIX PASS ---` heading. The fix agent addresses findings in `BACKEND_WT`.
- **Frontend fix**: use the same prompt as Phase 2 + append the frontend reviewer's findings
  under a `--- FIX PASS ---` heading. The fix agent addresses findings in `FRONTEND_WT`.

After fixes, run **one re-review pass** (same reviewer prompts, same parallel approach).
Do not loop again after the re-review — accept the result and proceed to Phase 4.

---

## Phase 4 — Quality checks (run both QA agents in parallel)

Spawn both QA subagents **in the same response**:

**Backend QA:** Read `.claude/agents/chairly-backend-qa.md` and spawn a subagent with that
content, appending:
```
--- CONTEXT ---
BACKEND_WT: {BACKEND_WT}
```

**Frontend QA:** Read `.claude/agents/chairly-frontend-qa.md` and spawn a subagent with that
content, appending:
```
--- CONTEXT ---
FRONTEND_WT: {FRONTEND_WT}
```

Wait for both. Parse their result blocks:
- `BACKEND-QA-RESULT` → check `status:` field
- `FRONTEND-QA-RESULT` → check `status:` field

### QA fix cycle

If either QA agent reported `status: fail`:
1. Read the `notes:` field from the failing result
2. Spawn a targeted fix subagent for that layer using the corresponding phase prompt
   + the QA notes appended under `--- QA FIX PASS ---`
3. Re-run only the failing QA agent (not both)
4. Repeat up to **2 more times** per layer
5. If still failing after 3 total attempts, proceed to Phase 5 but note the failure in the PR description

---

## Phase 5 — Merge and create PR

Read `.claude/skills/feature-team/phase-5-merge.md` and follow those instructions directly.
Execute the git and gh commands yourself — no subagent needed for this phase.

---

## Global rules

- Never implement features yourself — you are the orchestrator only
- Always read a phase file before spawning its subagent (do not use cached content)
- Always wait for a phase to complete before starting the next (except parallel spawns)
- Keep a running status log in your responses so the user can follow progress:
  `[Phase 0 ✓] [Phase 1 ✓] [Phase 2 running...] ...`
- If a subagent returns an error or unexpected output, re-spawn it once before escalating
