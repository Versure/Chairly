# Scoped Worktrees

> **Status: Implemented** — Merged to main.

## Overview

The `feature-team` skill and `rework.sh` use fixed worktree paths (`.worktrees/backend/` and `.worktrees/frontend/`). When two `/feature-team` instances run concurrently for different specs, they collide on the same worktree directories. The fix scopes each worktree to its feature name: `.worktrees/{feature-name}/backend/` and `.worktrees/{feature-name}/frontend/`. Fixes GitHub issue #36.

## Domain Context

- Bounded context: AI workflow tooling (not a product domain)
- Key entities involved: none
- Key files:
  - `.claude/skills/feature-team/SKILL.md` — defines `BACKEND_WT` and `FRONTEND_WT` variables and the worktree setup commands
  - `scripts/agent-team/rework.sh` — hardcodes `.worktrees/backend` and `.worktrees/frontend`
  - `scripts/agent-team/hooks/task-completed.sh` — hardcodes `.worktrees/backend` and `.worktrees/frontend`

---

## Backend Tasks

> NOTE: "Backend" here refers to the script/config layer, not dotnet. There are no API endpoints or Angular components in this feature.

### B1 — Update feature-team SKILL.md to use scoped worktree paths

**File:** `.claude/skills/feature-team/SKILL.md`

**Change 1 — Variable definitions (Step 0a):**

Find the block:
```
BACKEND_WT      = .worktrees/backend/
FRONTEND_WT     = .worktrees/frontend/
```

Replace with:
```
BACKEND_WT      = .worktrees/{FEATURE_NAME}/backend/
FRONTEND_WT     = .worktrees/{FEATURE_NAME}/frontend/
```

**Change 2 — Step 0c worktree setup commands:**

Find the remove-stale block:
```bash
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

Replace with:
```bash
# Remove stale worktrees if they exist
[ -d ".worktrees/{FEATURE_NAME}/backend" ] \
  && (git worktree remove --force ".worktrees/{FEATURE_NAME}/backend" 2>/dev/null || rm -rf ".worktrees/{FEATURE_NAME}/backend") \
  || true
[ -d ".worktrees/{FEATURE_NAME}/frontend" ] \
  && (git worktree remove --force ".worktrees/{FEATURE_NAME}/frontend" 2>/dev/null || rm -rf ".worktrees/{FEATURE_NAME}/frontend") \
  || true

# Create backend worktree
mkdir -p ".worktrees/{FEATURE_NAME}"
git rev-parse --verify "impl/{FEATURE_NAME}-backend" >/dev/null 2>&1 \
  && git worktree add ".worktrees/{FEATURE_NAME}/backend" "impl/{FEATURE_NAME}-backend" \
  || git worktree add -b "impl/{FEATURE_NAME}-backend" ".worktrees/{FEATURE_NAME}/backend" "feat/{FEATURE_NAME}"

# Create frontend worktree
git rev-parse --verify "impl/{FEATURE_NAME}-frontend" >/dev/null 2>&1 \
  && git worktree add ".worktrees/{FEATURE_NAME}/frontend" "impl/{FEATURE_NAME}-frontend" \
  || git worktree add -b "impl/{FEATURE_NAME}-frontend" ".worktrees/{FEATURE_NAME}/frontend" "feat/{FEATURE_NAME}"
```

**Change 3 — Log line (Step 0c):**

The log text does not need to change.

---

### B2 — Update rework.sh to use scoped worktree paths

**File:** `scripts/agent-team/rework.sh`

**Change 1 — Variable definitions:**

Find:
```bash
BACKEND_WT="${REPO_ROOT}/.worktrees/backend"
FRONTEND_WT="${REPO_ROOT}/.worktrees/frontend"
```

Replace with:
```bash
BACKEND_WT="${REPO_ROOT}/.worktrees/${FEATURE_NAME}/backend"
FRONTEND_WT="${REPO_ROOT}/.worktrees/${FEATURE_NAME}/frontend"
```

**Change 2 — Add mkdir before worktree creation:**

Find the section that removes stale worktrees and re-creates them:
```bash
if [[ -d "$BACKEND_WT" ]]; then
  echo "Removing stale backend worktree..."
  git worktree remove --force "$BACKEND_WT" 2>/dev/null || rm -rf "$BACKEND_WT"
fi
if [[ -d "$FRONTEND_WT" ]]; then
  echo "Removing stale frontend worktree..."
  git worktree remove --force "$FRONTEND_WT" 2>/dev/null || rm -rf "$FRONTEND_WT"
fi
```

After the removal block, before the `git worktree add` calls, add:
```bash
mkdir -p "${REPO_ROOT}/.worktrees/${FEATURE_NAME}"
```

---

### B3 — Update task-completed.sh hook to use scoped worktree paths

**File:** `scripts/agent-team/hooks/task-completed.sh`

The hook currently uses hardcoded worktree paths. Since the hook runs in the context of a specific feature, it needs to know the feature name to derive the correct worktree path.

**Strategy:** instead of hardcoding the path, detect the active worktree by looking for a match using `find`. The hook already knows the repo root; it can find the correct worktree by scanning `.worktrees/` subdirectories.

**Change — Replace the hardcoded worktree paths with dynamic discovery:**

Find the section:
```bash
if [[ "$LAYER" == "backend" ]]; then
  WORKTREE="${REPO_ROOT}/.worktrees/backend"

  if [[ ! -d "$WORKTREE" ]]; then
    exit 0
  fi
  ...
elif [[ "$LAYER" == "frontend" ]]; then
  WORKTREE="${REPO_ROOT}/.worktrees/frontend"

  if [[ ! -d "$WORKTREE" ]]; then
    exit 0
  fi
  ...
```

Replace with dynamic discovery:

```bash
if [[ "$LAYER" == "backend" ]]; then
  # Find the first active backend worktree under .worktrees/*/backend
  WORKTREE=$(find "${REPO_ROOT}/.worktrees" -maxdepth 2 -name "backend" -type d 2>/dev/null | head -1)

  if [[ -z "$WORKTREE" || ! -d "$WORKTREE" ]]; then
    exit 0
  fi
  ...
elif [[ "$LAYER" == "frontend" ]]; then
  # Find the first active frontend worktree under .worktrees/*/frontend
  WORKTREE=$(find "${REPO_ROOT}/.worktrees" -maxdepth 2 -name "frontend" -type d 2>/dev/null | head -1)

  if [[ -z "$WORKTREE" || ! -d "$WORKTREE" ]]; then
    exit 0
  fi
  ...
```

**Note:** `find ... | head -1` picks the first match. When only one feature-team runs, this correctly finds `.worktrees/{feature-name}/backend`. When multiple run simultaneously, this is still best-effort (the hook is a build check, not a critical path).

---

## Acceptance Criteria

- [ ] `BACKEND_WT` in `feature-team/SKILL.md` is `.worktrees/{FEATURE_NAME}/backend/`
- [ ] `FRONTEND_WT` in `feature-team/SKILL.md` is `.worktrees/{FEATURE_NAME}/frontend/`
- [ ] All `git worktree add/remove` commands in SKILL.md use the scoped paths
- [ ] `rework.sh` uses `${REPO_ROOT}/.worktrees/${FEATURE_NAME}/backend` and `.../frontend`
- [ ] `mkdir -p` is called before creating scoped worktrees (in both SKILL.md and rework.sh)
- [ ] `task-completed.sh` discovers the worktree dynamically instead of using a hardcoded path
- [ ] Running two `/feature-team` instances for different specs does not cause worktree path collisions
- [ ] No dotnet or Angular code is changed by this spec

## Out of Scope

- Adding worktree cleanup automation after a feature is merged
- Parallelising backend and frontend phases within a single feature (separate concern)
- Changes to CI/CD pipelines
