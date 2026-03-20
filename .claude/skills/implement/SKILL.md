---
name: implement
description: >
  Orchestrate a full feature implementation. Creates worktrees, spawns backend + frontend
  impl agents, runs review and QA, merges and creates PR. Usage: /implement {feature-name}
user-invocable: true
---

# /implement — Feature Implementation Workflow

You are the **lead orchestrator**. You coordinate all phases by spawning subagents.
Never implement features yourself — your job is to orchestrate, verify, and hand off.

---

## Step 0 — Parse input and verify

### 0a — Parse input

`$ARGUMENTS` is the feature name (kebab-case).

Set variables:
```
FEATURE_NAME    = $ARGUMENTS
SPEC_DIR        = .claude/tasks/{FEATURE_NAME}/
SPEC_PATH       = .claude/tasks/{FEATURE_NAME}/spec.md
TASKS_PATH      = .claude/tasks/{FEATURE_NAME}/tasks.json
FEATURE_BRANCH  = feat/{FEATURE_NAME}
BACKEND_BRANCH  = impl/{FEATURE_NAME}-backend
FRONTEND_BRANCH = impl/{FEATURE_NAME}-frontend
BACKEND_WT      = .worktrees/{FEATURE_NAME}/backend/
FRONTEND_WT     = .worktrees/{FEATURE_NAME}/frontend/
```

### 0b — Verify spec exists

Verify `SPEC_PATH` and `TASKS_PATH` exist on the current branch. If either is missing:
> Error: Spec not found. Run `/create-spec {FEATURE_NAME}` first and merge the spec PR.

### 0c — Verify we are on main

```bash
git rev-parse --abbrev-ref HEAD
```

If not `main`, abort:
> Error: /implement must be run from the main branch. Run `git checkout main && git pull origin main` first.

### 0d — Set up git branches and worktrees

```bash
# Create feature branch if it doesn't exist
git rev-parse --verify "feat/{FEATURE_NAME}" >/dev/null 2>&1 \
  || git branch "feat/{FEATURE_NAME}"

# Remove stale worktrees
[ -d ".worktrees/{FEATURE_NAME}/backend" ] \
  && (git worktree remove --force ".worktrees/{FEATURE_NAME}/backend" 2>/dev/null || rm -rf ".worktrees/{FEATURE_NAME}/backend") \
  || true
[ -d ".worktrees/{FEATURE_NAME}/frontend" ] \
  && (git worktree remove --force ".worktrees/{FEATURE_NAME}/frontend" 2>/dev/null || rm -rf ".worktrees/{FEATURE_NAME}/frontend") \
  || true

# Create worktrees
mkdir -p ".worktrees/{FEATURE_NAME}"
git rev-parse --verify "impl/{FEATURE_NAME}-backend" >/dev/null 2>&1 \
  && git worktree add ".worktrees/{FEATURE_NAME}/backend" "impl/{FEATURE_NAME}-backend" \
  || git worktree add -b "impl/{FEATURE_NAME}-backend" ".worktrees/{FEATURE_NAME}/backend" "feat/{FEATURE_NAME}"

git rev-parse --verify "impl/{FEATURE_NAME}-frontend" >/dev/null 2>&1 \
  && git worktree add ".worktrees/{FEATURE_NAME}/frontend" "impl/{FEATURE_NAME}-frontend" \
  || git worktree add -b "impl/{FEATURE_NAME}-frontend" ".worktrees/{FEATURE_NAME}/frontend" "feat/{FEATURE_NAME}"
```

Log: `[Step 0 done] Feature: {FEATURE_NAME} | Branch: feat/{FEATURE_NAME} | Worktrees ready`

---

## Phase 0.5 — Infrastructure implementation (if infra tasks exist)

Read `TASKS_PATH`. If there are tasks where `"layer": "infra"`, run this phase first.
If there are no infra tasks, skip directly to Phase 1.

**Infra tasks run before backend/frontend** because they set up services that backend
code depends on (Aspire resources, Keycloak config, RabbitMQ topology, etc.).

1. Read `.claude/skills/implement/phase-infra.md`
2. Spawn **infra-impl** agent (`subagent_type: infra-impl`) with that content, appending:
```
--- CONTEXT ---
SPEC_PATH:      {SPEC_PATH}
TASKS_PATH:     {TASKS_PATH}
BACKEND_WT:     {BACKEND_WT}
Infra tasks:    {list each infra task as "I1 — {title}"}
```

3. Wait for completion.
4. Read `.claude/skills/implement/phase-infra-review.md`
5. Spawn **infra-reviewer** agent (`subagent_type: infra-reviewer`) with content, appending:
```
--- CONTEXT ---
SPEC_PATH:    {SPEC_PATH}
BACKEND_WT:   {BACKEND_WT}
```

6. If findings, spawn infra-impl with findings under `--- FIX PASS ---`, then one re-review.

---

## Phase 1 — Implementation (backend + frontend in parallel)

Read `TASKS_PATH` to get the task lists. Spawn both implementation agents **in the same response**:

**Backend:**
1. Read `.claude/skills/implement/phase-backend.md`
2. Spawn **backend-impl** agent (`subagent_type: backend-impl`) with that content, appending:
```
--- CONTEXT ---
SPEC_PATH:      {SPEC_PATH}
TASKS_PATH:     {TASKS_PATH}
BACKEND_WT:     {BACKEND_WT}
Backend tasks:  {list each backend task as "B1 — {title}"}
```

**Frontend:**
1. Read `.claude/skills/implement/phase-frontend.md`
2. Spawn **frontend-impl** agent (`subagent_type: frontend-impl`) with that content, appending:
```
--- CONTEXT ---
SPEC_PATH:      {SPEC_PATH}
TASKS_PATH:     {TASKS_PATH}
FRONTEND_WT:    {FRONTEND_WT}
Frontend tasks: {list each frontend task as "F1 — {title}"}
```

Wait for both to complete.

---

## Phase 2 — Code review (both reviewers in parallel)

Spawn both review agents **in the same response**:

**Backend reviewer:**
1. Read `.claude/skills/implement/phase-backend-review.md`
2. Spawn **backend-reviewer** agent (`subagent_type: backend-reviewer`) with content, appending:
```
--- CONTEXT ---
SPEC_PATH:    {SPEC_PATH}
BACKEND_WT:   {BACKEND_WT}
```

**Frontend reviewer:**
1. Read `.claude/skills/implement/phase-frontend-review.md`
2. Spawn **frontend-reviewer** agent (`subagent_type: frontend-reviewer`) with content, appending:
```
--- CONTEXT ---
SPEC_PATH:    {SPEC_PATH}
FRONTEND_WT:  {FRONTEND_WT}
```

Wait for both.

### Fix cycle (if findings exist)

If a reviewer returned issues, spawn fix agents (parallel if both need fixes):
- **Backend fix**: same prompt as Phase 1 + append findings under `--- FIX PASS ---`
- **Frontend fix**: same prompt as Phase 1 + append findings under `--- FIX PASS ---`
- **Infra fix**: same prompt as Phase 0.5 + append findings under `--- FIX PASS ---`

After fixes, run **one re-review pass**. Do not loop again — accept and proceed.

---

## Phase 3 — Quality checks (both QA agents in parallel)

Spawn both QA agents **in the same response**:

**Backend QA:** Spawn `chairly-backend-qa` agent, appending:
```
--- CONTEXT ---
BACKEND_WT: {BACKEND_WT}
```

**Frontend QA:** Spawn `chairly-frontend-qa` agent, appending:
```
--- CONTEXT ---
FRONTEND_WT: {FRONTEND_WT}
```

Parse `BACKEND-QA-RESULT` and `FRONTEND-QA-RESULT`.

### QA fix cycle

If either QA agent reported `status: fail`:
1. Spawn a fix agent for that layer with QA notes under `--- QA FIX PASS ---`
2. Re-run only the failing QA agent
3. Repeat up to **2 more times** per layer
4. If still failing after 3 attempts, note in PR description

---

## Phase 4 — Merge and create PR

Read `.claude/skills/implement/phase-merge.md` and follow those instructions directly.
Execute git and gh commands yourself — no subagent needed.

---

## Global rules

- Never implement features yourself — orchestrate only
- Always read a phase file before spawning its agent
- Always wait for a phase to complete before starting the next (except parallel spawns)
- Keep a running status log: `[Phase 1 running...] [Phase 2 done] ...`
- If a subagent returns an error, re-spawn once before escalating
