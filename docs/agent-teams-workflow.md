# Agent Teams Workflow

Multi-agent feature implementation workflow using Claude Code's experimental agent teams feature.

## Overview

Two skills drive the full lifecycle:

- `/feature-team [description or path]` — implements a feature end-to-end and opens a PR
- `/rework-team [pr-number]` — reads your PR review comments and reworks the implementation

Both run fully autonomously once triggered. You review only at the PR stage.

---

## Architecture

```
You
 |
 | ./scripts/agent-team/start.sh "Add booking cancellation"
 |
[Lead session — tmux pane 0]
 |
 |-- Phase 1: Spec
 |    └── [Spec agent — pane 1]
 |         Uses: chairly-explorer subagent (Haiku) to research existing code
 |         Writes: .claude/tasks/{feature}/spec.md
 |                 .claude/tasks/{feature}/tasks.json  (committed to branch)
 |
 |-- Phase 2: Implementation (parallel, each in own git worktree)
 |    ├── [Backend agent — pane 1]
 |    |    Worktree: .worktrees/backend  (branch: impl/{name}-backend)
 |    |    All file ops prefixed: .worktrees/backend/
 |    |    Uses: chairly-explorer subagent for pattern lookups
 |    |    Implements: Domain, Infrastructure, Api slices, tests
 |    |
 |    └── [Frontend agent — pane 2]
 |         Worktree: .worktrees/frontend  (branch: impl/{name}-frontend)
 |         All file ops prefixed: .worktrees/frontend/
 |         Uses: chairly-explorer subagent for pattern lookups
 |         Implements: service, store, components, routes, e2e tests
 |
 |-- Phase 3: Review (parallel)
 |    ├── [Backend reviewer — pane 1]
 |    |    Reviews backend worktree diff vs spec
 |    |    Reports issues to LEAD (not directly to backend agent)
 |    |
 |    └── [Frontend reviewer — pane 2]
 |         Reviews frontend worktree diff vs spec
 |         Reports issues to LEAD (not directly to frontend agent)
 |         Lead spawns targeted fix agents if issues found, then re-reviews
 |
 |-- Phase 4: QA
 |    └── [QA agent — pane 1]
 |         Spawns in parallel:
 |           chairly-backend-qa subagent → dotnet build/test/format
 |           chairly-frontend-qa subagent → nx lint/test/build/e2e
 |         Writes missing tests; messages LEAD on failure
 |
 |-- Lead merges worktree branches → feat/{name}
 |-- Lead opens PR → you review
```

---

## Agent Teams vs. Subagents

These are separate mechanisms that complement each other:

| | Agent teams | Subagents |
|---|---|---|
| What they are | Parallel, independent Claude Code sessions | Workers spawned within a single session |
| Communication | Peer-to-peer mailbox + shared task list | Report results back to parent only |
| Nesting | Teammates cannot spawn teammates | Subagents cannot spawn subagents |
| Use in this workflow | Lead + 5 role-based teammates | Each teammate spawns subagents for focused sub-tasks |

Agent teams coordinate phases. Subagents handle focused, context-heavy sub-tasks within each teammate's session.

---

## Critical Design Constraint: Worktree Path Discipline

Agent teams have no built-in way to set a per-teammate working directory. All teammates start at the repo root. This means the backend and frontend agents must be explicitly disciplined about where they write files.

**Rule:** Phase spawn prompts must tell each implementation agent:

> "Your working directory is the repo root. ALL your file operations must use paths prefixed with `.worktrees/backend/` (or `.worktrees/frontend/`). Never read from or write to `src/backend/` or `src/frontend/` directly — always go through your worktree path. For Bash commands that operate on the project (builds, migrations, git), always `cd .worktrees/{layer}` first."

**EF Core migration command from inside backend worktree:**
```bash
cd .worktrees/backend && dotnet ef migrations add {Name} \
  --project src/backend/Chairly.Infrastructure \
  --startup-project src/backend/Chairly.Api
```
(Paths are relative to the worktree root, which matches the normal repo layout.)

**Git operations from inside worktree:**
```bash
cd .worktrees/backend && git add -p && git commit -m "feat(bookings): ..."
```

This constraint is enforced purely through the spawn prompt wording in `backend-impl.md` and `frontend-impl.md`.

**tasks.json read vs. write paths:**
The spec agent commits `tasks.json` to the main feature branch (`feat/{name}`), which lives at the repo root. Worktrees are created before the spec is written, so they do NOT contain `tasks.json`. Implementation agents must read it from the **repo root path** (`.claude/tasks/{name}/tasks.json`), not from inside their worktree. The phase spawn prompts must make this explicit:

> "Read tasks.json from `.claude/tasks/{name}/tasks.json` (repo root). Write all code into `.worktrees/{layer}/`."

---

## Files to Create

```
.claude/
  skills/
    feature-team/
      SKILL.md                         # Lead orchestration prompt (invoke: /feature-team)
      phase-0-spec.md                  # Spec agent spawn prompt
      phase-1-backend.md               # Backend agent spawn prompt (enforces worktree paths)
      phase-2-frontend.md              # Frontend agent spawn prompt (enforces worktree paths)
      phase-3-backend-review.md        # Backend reviewer spawn prompt
      phase-3-frontend-review.md       # Frontend reviewer spawn prompt
      phase-5-merge.md                 # Merge + PR instructions
    rework-team/
      SKILL.md                         # Rework lead orchestration prompt (invoke: /rework-team)
      phase-rework-backend.md          # Backend rework agent spawn prompt
      phase-rework-frontend.md         # Frontend rework agent spawn prompt
    chairly-backend-slice/
      SKILL.md                         # Backend code patterns reference (user-invocable: false)
    chairly-frontend-domain/
      SKILL.md                         # Frontend code patterns reference (user-invocable: false)
    chairly-spec-format/
      SKILL.md                         # Spec + tasks.json format guide (user-invocable: false)

  agents/
    chairly-explorer.md                # Haiku read-only codebase explorer subagent
    chairly-backend-qa.md              # Backend quality checks subagent
    chairly-frontend-qa.md             # Frontend quality checks subagent

  settings.json                        # Project-level hooks (TaskCompleted)

scripts/
  agent-team/
    start.sh                           # WSL: enable env var, start tmux, launch Claude + invoke skill
    rework.sh                          # WSL: same but for rework run
    hooks/
      task-completed.sh                # Hook: runs build check in correct worktree on task complete

docs/
  agent-teams-workflow.md              # This file
```

---

## Reference Skills

Three project-scoped skills in `.claude/skills/`. Available to all teammates automatically. All use `user-invocable: false` — they are invisible in the `/` menu but Claude loads them when their description matches the current task.

### `chairly-backend-slice`

**Frontmatter:**
```yaml
name: chairly-backend-slice
description: >
  Chairly backend patterns. Use when implementing VSA slices, handlers,
  EF Core entities, or tests in the Chairly backend.
user-invocable: false
```

Contains concrete boilerplate (not just rules) for:
- Complete VSA slice: command, handler, endpoint — with all required pragmas (`CA1812`, `MA0026`, `ConfigureAwait(false)`)
- Entity: `TenantId`, `CreatedAtUtc`, `CreatedBy` with `MA0026` pragma comment
- OneOf result pattern: union return declaration, `AsT0`/`IsT1` access in handlers and tests
- `IEntityTypeConfiguration<T>` shape for Infrastructure
- EF Core migration command (paths relative to worktree root — see worktree section above)
- Unit test pattern: in-memory DbContext with `Guid.NewGuid()` database name, happy path + not-found + validation
- `TenantConstants.DefaultTenantId` usage

### `chairly-frontend-domain`

**Frontmatter:**
```yaml
name: chairly-frontend-domain
description: >
  Chairly frontend patterns. Use when implementing Angular features, stores,
  services, or components in the Chairly frontend.
user-invocable: false
```

Contains concrete boilerplate for:
- API service: `@Injectable({ providedIn: 'root' })`, `inject(API_BASE_URL)`, `inject(HttpClient)`, `Observable<T>` returns
- SignalStore: `withState`, `withComputed`, `withMethods`, `patchState`, `take(1).subscribe()` pattern
- Smart component: `ChangeDetectionStrategy.OnPush`, `inject()`, `computed()`, `viewChild.required()`, `templateUrl:` only
- Route config: lazy-loaded at domain root (`{domain}.routes.ts`)
- Barrel index: what to export from each layer's `index.ts`
- Dutch UI text examples

### `chairly-spec-format`

**Frontmatter:**
```yaml
name: chairly-spec-format
description: >
  Chairly spec format. Use when writing a feature spec or tasks.json
  for a Chairly feature.
user-invocable: false
```

Contains:
- Exact spec markdown structure (sections: Context, Domain model changes, Backend contracts, Frontend flows, Acceptance criteria)
- Exact tasks.json schema with field descriptions
- File ownership: backend agent owns `src/backend/` (via `.worktrees/backend/`), frontend agent owns `src/frontend/` (via `.worktrees/frontend/`)
- Task granularity guide: one task = one slice, one component, or one test file
- Ubiquitous language reminders (Booking not appointment, Client not customer, etc.)

---

## Custom Subagents

Three project-scoped subagents in `.claude/agents/`. Teammates spawn these for focused sub-tasks.

### `chairly-explorer`

```markdown
---
name: chairly-explorer
description: >
  Read-only Chairly codebase explorer. Use proactively whenever you need to
  research existing patterns, find files, or understand how something is
  implemented before writing new code.
model: haiku
tools: Read, Grep, Glob
---

You are a read-only codebase research agent for the Chairly project.
When invoked, explore the codebase to answer the question or find the
patterns requested. Return a concise summary of findings with specific
file paths and line references. Never suggest edits.
```

**Note:** Pattern skills (`chairly-backend-slice`, `chairly-frontend-domain`) are NOT preloaded here. Haiku's context is limited and the full boilerplate would crowd it. Instead, the implementor teammates (running Sonnet/Opus) use `chairly-explorer` for file discovery, and load the pattern skills themselves from their own sessions.

**Used by:** Spec agent, backend agent, frontend agent, backend reviewer, frontend reviewer.

### `chairly-backend-qa`

```markdown
---
name: chairly-backend-qa
description: >
  Run backend quality checks for Chairly in the backend worktree. Runs
  dotnet build, dotnet test, and dotnet format --verify-no-changes.
  Returns a pass/fail summary with failure output only.
model: inherit
tools: Bash, Read
---

Run all backend quality checks from within the backend worktree directory.
You will be given the worktree path. Run:
  cd {worktree_path} && dotnet build src/backend/Chairly.slnx
  cd {worktree_path} && dotnet test src/backend/Chairly.slnx
  cd {worktree_path} && dotnet format src/backend/Chairly.slnx --verify-no-changes
Return PASS or FAIL, and on failure include only the relevant error output.
```

### `chairly-frontend-qa`

```markdown
---
name: chairly-frontend-qa
description: >
  Run frontend quality checks for Chairly in the frontend worktree. Runs
  nx lint, nx format:check, nx test, nx build, and nx e2e.
  Returns a pass/fail summary with failure output only.
model: inherit
tools: Bash, Read
---

Run all frontend quality checks from within the frontend worktree directory.
You will be given the worktree path. Run from {worktree_path}/src/frontend/chairly/:
  npx nx affected -t lint --base=main
  npx nx format:check --base=main
  npx nx affected -t test --base=main
  npx nx affected -t build --base=main
  npx nx e2e chairly-e2e
Return PASS or FAIL, and on failure include only the relevant error output.
```

---

## Phase Breakdown

### Phase 0 — Setup (Lead)

```bash
git checkout -b feat/{feature-name} main
git worktree add .worktrees/backend -b impl/{name}-backend
git worktree add .worktrees/frontend -b impl/{name}-frontend
```

### Phase 1 — Spec (1 teammate)

The spec agent:
1. Spawns `chairly-explorer` to research existing domain entities and patterns relevant to the feature
2. Produces two artifacts in `.claude/tasks/{feature-name}/`:

**`spec.md`** — human-readable spec committed to the branch.

**`tasks.json`** — machine-readable task list, also committed to the branch so all subsequent teammates can read it:

```json
{
  "feature": "booking-cancellation",
  "specPath": ".claude/tasks/booking-cancellation/spec.md",
  "tasks": [
    {
      "id": "B1",
      "layer": "backend",
      "title": "Add cancellation fields to Booking entity",
      "status": "pending",
      "dependsOn": []
    },
    {
      "id": "B2",
      "layer": "backend",
      "title": "Add CancelBooking endpoint and handler",
      "status": "pending",
      "dependsOn": ["B1"]
    },
    {
      "id": "F1",
      "layer": "frontend",
      "title": "Add cancel method to booking API service",
      "status": "pending",
      "dependsOn": ["B2"]
    },
    {
      "id": "F2",
      "layer": "frontend",
      "title": "Add cancellation flow to booking list page",
      "status": "pending",
      "dependsOn": ["B2"]
    }
  ]
}
```

The spec agent commits both files to the feature branch (`feat/{name}`) and messages the lead with the tasks.json path.

### Phase 2 — Implementation (2 parallel teammates)

Backend and frontend agents run simultaneously, each in their own worktree.

**Backend agent** receives in its spawn prompt:
- Path to tasks.json
- Worktree path: `.worktrees/backend`
- Explicit instruction: all file operations and git commands must use `.worktrees/backend/` prefix

**Frontend agent** receives in its spawn prompt:
- Path to tasks.json
- Worktree path: `.worktrees/frontend`
- Explicit instruction: all file operations and git commands must use `.worktrees/frontend/` prefix

Each agent uses `chairly-explorer` for pattern lookups and commits after each task.

### Phase 3 — Review (2 parallel teammates)

After both implementation agents are done, the lead spawns two reviewers in parallel.

**Backend reviewer:**
1. Reads the spec and all changed files under `.worktrees/backend/`
2. Checks: slice structure, result pattern, validation, tests, naming, no logic in endpoints
3. Reports findings inline using the `BACKEND-REVIEW-RESULT` block (no files written)

**Frontend reviewer:**
1. Reads the spec and all changed files under `.worktrees/frontend/`
2. Checks: signal APIs, OnPush, Dutch UI text, smart/presentational split, store usage, test coverage
3. Reports findings inline using the `FRONTEND-REVIEW-RESULT` block (no files written)

**Lead's response to review findings:**
- If no issues: proceeds to QA
- If issues found: spawns a **fix agent** using the Phase 1/2 implementation prompt with a `--- FIX PASS ---` block appended. The fix agent applies corrections in the worktree, commits, and reports back. The lead re-runs one review pass to confirm.

This routes all fix coordination through the lead rather than relying on reviewers directly messaging potentially-idle implementation agents.

### Phase 4 — QA (lead spawns both QA subagents)

The lead spawns both QA subagents in parallel:
```
chairly-backend-qa  (worktree: .worktrees/backend)
chairly-frontend-qa (worktree: .worktrees/frontend)
```

If either fails:
1. Lead spawns a fix agent for the affected layer using the implementation phase prompt
2. After fix, the lead re-runs only the failing QA subagent
3. Retry up to 2 more times per layer, then proceed with a known-issues note

### Phase 5 — Merge and PR (Lead)

```bash
# On feat/{name} branch:
git merge impl/{name}-backend --no-ff -m "chore: merge backend implementation"
git merge impl/{name}-frontend --no-ff -m "chore: merge frontend implementation"

# Commit spec + tasks
git add .claude/tasks/{name}/
git diff --cached --quiet || git commit -m "chore({name}): add feature spec and tasks"

# Clean up worktrees:
git worktree remove .worktrees/backend
git worktree remove .worktrees/frontend
git branch -d impl/{name}-backend impl/{name}-frontend

# Push and open PR:
git push -u origin feat/{name}
gh pr create \
  --title "feat({context}): {feature description}" \
  --body "..."
```

PR body includes: link to spec, agent implementation summary, QA results.

---

## Rework Workflow

```bash
# From WSL:
./scripts/agent-team/rework.sh 42
```

Or from inside Claude Code:
```
/rework-team 42
```

The rework flow:
1. `rework.sh` fetches PR comments and writes `.claude/tasks/{feature}/pr-comments.md`
2. `rework.sh` checks out the feature branch and recreates worktrees
3. `/rework-team` reads `pr-comments.md`, categorizes comments by layer, and spawns fix agents
4. QA subagents run in parallel; failing layers get a fix pass + re-run
5. The lead merges fixes back into the feature branch and pushes

---

## Skill Design

### `/feature-team`

```yaml
name: feature-team
description: >
  Orchestrate a full feature implementation using the agent team workflow.
  Runs spec -> backend -> frontend -> review -> QA -> merge/PR phases.
user-invocable: true
```

Lead orchestration steps:
1. Parse `$ARGUMENTS`: if it resolves to an existing file path, read it; otherwise treat as free-form
2. Derive kebab-case `{feature-name}`
3. Verify `main` is checked out, create `feat/{feature-name}` if missing
4. Create worktrees: `impl/{name}-backend` and `impl/{name}-frontend`
5. Read `phase-0-spec.md`; spawn spec agent with that prompt + input
6. Wait for spec + tasks, then read `phase-1-backend.md` and `phase-2-frontend.md`; spawn both in parallel
7. Read `phase-3-backend-review.md` and `phase-3-frontend-review.md`; spawn both reviewers in parallel and run fix pass if needed
8. Spawn QA subagents from `.claude/agents/chairly-backend-qa.md` and `.claude/agents/chairly-frontend-qa.md`; re-run failing layers after fixes
9. Read `phase-5-merge.md` and follow merge + PR instructions

### `/rework-team`

```yaml
name: rework-team
description: >
  Orchestrate a rework pass based on PR review comments.
user-invocable: true
```

Lead orchestration steps:
1. `gh pr view $ARGUMENTS --json headRefName,title` to resolve the feature branch
2. Verify `.claude/tasks/{feature}/pr-comments.md` exists (written by `rework.sh`)
3. Categorize comments by layer and spawn fix agents with the rework phase prompts
4. Run QA subagents in parallel; re-run failing layers after fixes
5. Merge worktree fixes into the feature branch, push, and comment on the PR

---

## Shell Scripts

### `scripts/agent-team/start.sh`

```bash
#!/usr/bin/env bash
# Usage (from WSL): ./scripts/agent-team/start.sh "feature description"
#              or:  ./scripts/agent-team/start.sh docs/briefs/my-feature.md

set -euo pipefail

REPO_ROOT="$(git rev-parse --show-toplevel)"
FEATURE_INPUT="$*"

if [[ -z "$FEATURE_INPUT" ]]; then
  echo "Usage: start.sh <description or path/to/brief.md>" >&2
  exit 1
fi

tmux new-session -d -s feature-team -c "$REPO_ROOT"
tmux send-keys -t feature-team \
  "CLAUDE_CODE_EXPERIMENTAL_AGENT_TEAMS=1 claude --dangerously-skip-permissions" Enter

# Wait for Claude to finish starting up, then send the skill invocation
# Increase to sleep 8 if the command isn't received (slow WSL or cold start)
sleep 6
tmux send-keys -t feature-team "/feature-team $FEATURE_INPUT" Enter

# Attach to the session so you can watch progress
tmux attach-session -t feature-team
```

### `scripts/agent-team/rework.sh`

```bash
#!/usr/bin/env bash
# Usage (from WSL): ./scripts/agent-team/rework.sh <pr-number>

set -euo pipefail

PR_NUMBER="${1:-}"

if [[ -z "$PR_NUMBER" ]]; then
  echo "Usage: rework.sh <pr-number>" >&2
  exit 1
fi

# Fetch PR comments into .claude/tasks/{feature}/pr-comments.md
# Checkout feature branch and recreate worktrees

REPO_ROOT="$(git rev-parse --show-toplevel)"
tmux new-session -d -s rework-team -c "$REPO_ROOT"
tmux send-keys -t rework-team \
  "claude --dangerously-skip-permissions" Enter

sleep 6
tmux send-keys -t rework-team "/rework-team $PR_NUMBER" Enter

tmux attach-session -t rework-team
```

---

## Quality Gates

### `TaskCompleted` hook

Configured in `.claude/settings.json`. Runs `scripts/agent-team/hooks/task-completed.sh` whenever a teammate marks a task complete.

```json
{
  "env": {
    "CLAUDE_CODE_EXPERIMENTAL_AGENT_TEAMS": "1"
  },
  "hooks": {
    "TaskCompleted": [
      {
        "hooks": [
          {
            "type": "command",
            "command": "scripts/agent-team/hooks/task-completed.sh"
          }
        ]
      }
    ]
  }
}
```

The hook script reads task JSON from stdin, determines the layer from the task ID prefix (`B` = backend, `F` = frontend), and runs a fast build check in the corresponding worktree:

```bash
#!/usr/bin/env bash
# scripts/agent-team/hooks/task-completed.sh
# Runs a quick build check in the correct worktree when a task is marked complete.
# Exit code 2 = block the task completion and send stderr feedback to the agent.

set -euo pipefail

# jq is required to parse hook input — skip silently if not available
command -v jq >/dev/null 2>&1 || exit 0

INPUT=$(cat)

# NOTE: The exact JSON field path for the task ID (.task.id) is based on the
# TaskCompleted hook schema as documented. Verify against a real run and adjust
# the jq path if the hook silently does nothing (i.e. TASK_ID is always empty).
TASK_ID=$(echo "$INPUT" | jq -r '.task.id // empty')

if [[ -z "$TASK_ID" ]]; then
  exit 0  # No task ID — not a task completion we can validate
fi

REPO_ROOT="$(git rev-parse --show-toplevel)"

if [[ "$TASK_ID" == B* ]]; then
  WORKTREE="$REPO_ROOT/.worktrees/backend"
  if [[ ! -d "$WORKTREE" ]]; then exit 0; fi
  if ! (cd "$WORKTREE" && dotnet build src/backend/Chairly.slnx --nologo --verbosity minimal 2>&1); then
    echo "Backend build failed in worktree. Fix build errors before marking task complete." >&2
    exit 2
  fi
elif [[ "$TASK_ID" == F* ]]; then
  WORKTREE="$REPO_ROOT/.worktrees/frontend"
  if [[ ! -d "$WORKTREE" ]]; then exit 0; fi
  if ! (cd "$WORKTREE/src/frontend/chairly" && npx nx affected -t lint --base=main 2>&1); then
    echo "Frontend lint failed in worktree. Fix lint errors before marking task complete." >&2
    exit 2
  fi
fi

exit 0
```

**Note:** The `TeammateIdle` hook was considered but dropped. It has no reliable way to know which worktree to check without per-teammate identity data. Lead prompt instructions handle idle behaviour instead.

---

## Evals (Regression Checks)

Run the lightweight evals script to validate agent and skill config, docs alignment,
and `.claude/tasks/*/tasks.json` schema:

```bash
./scripts/agent-team/evals/run.sh
```

This script is intended to run locally before changes and in CI on PRs that touch
`.claude/`, `scripts/agent-team/`, or `docs/agent-teams-*.md`.

---

## Git Worktree Strategy

```bash
# Phase 0 — Lead creates worktrees:
git worktree add .worktrees/backend -b impl/{name}-backend
git worktree add .worktrees/frontend -b impl/{name}-frontend

# Phase 2 — agents commit inside their worktree:
cd .worktrees/backend && git add ... && git commit -m "..."
cd .worktrees/frontend && git add ... && git commit -m "..."

# Phase 5 — Lead merges on feat/{name} branch:
git merge impl/{name}-backend --no-ff -m "chore: merge backend implementation"
git merge impl/{name}-frontend --no-ff -m "chore: merge frontend implementation"

# Cleanup:
git worktree remove .worktrees/backend
git worktree remove .worktrees/frontend
git branch -d impl/{name}-backend impl/{name}-frontend
```

`.worktrees/` is listed in `.gitignore`. `.claude/tasks/` is **NOT** gitignored — tasks.json is committed to the feature branch so all teammates can read it.

---

## Known Limitations and Mitigations

| Limitation | Mitigation |
|---|---|
| No per-teammate working directory | Phase spawn prompts enforce `.worktrees/{layer}/` path prefix on all file ops and Bash commands. Backend and frontend file trees are completely separate, so drift is caught quickly by the `TaskCompleted` hook. |
| No session resumption after crash | Shell scripts use tmux — detach/reattach survives. For hard crashes: rerun the script. The lead detects existing branch/spec artifacts and skips completed phases. |
| Task status can lag | Lead is instructed to poll task status after a teammate signals done. `TaskCompleted` hook acts as an additional gate. |
| Reviewer cannot reliably message idle agents | Reviewers report to the lead, not to implementation agents. Lead spawns fresh fix agents if needed. |
| QA subagents cannot spawn sub-subagents | Lead spawns QA subagents directly; they are terminal workers and do not spawn further agents. Fix cycles are coordinated by the lead. |
| Token cost | Max 2–3 teammates active simultaneously. Haiku for exploration. Total: 5 teammate contexts across the full run, never all at once. |
| Split panes require tmux | Scripts enforce WSL + tmux. In-process mode (Shift+Down) works as fallback in any terminal. |

---

## Implementation Steps

Create in this order — each group depends on the previous:

### Group 1 — Reference skills
1. `.claude/skills/chairly-backend-slice/SKILL.md`
2. `.claude/skills/chairly-frontend-domain/SKILL.md`
3. `.claude/skills/chairly-spec-format/SKILL.md`

### Group 2 — Custom subagents
4. `.claude/agents/chairly-explorer.md`
5. `.claude/agents/chairly-backend-qa.md`
6. `.claude/agents/chairly-frontend-qa.md`

### Group 3 — Feature team phase prompts
7. `.claude/skills/feature-team/phase-0-spec.md`
8. `.claude/skills/feature-team/phase-1-backend.md`
9. `.claude/skills/feature-team/phase-2-frontend.md`
10. `.claude/skills/feature-team/phase-3-backend-review.md`
11. `.claude/skills/feature-team/phase-3-frontend-review.md`
12. `.claude/skills/feature-team/phase-5-merge.md`
13. `.claude/skills/feature-team/SKILL.md`

### Group 4 — Rework phase prompts
14. `.claude/skills/rework-team/phase-rework-backend.md`
15. `.claude/skills/rework-team/phase-rework-frontend.md`
16. `.claude/skills/rework-team/SKILL.md`

### Group 5 — Infrastructure
17. `scripts/agent-team/hooks/task-completed.sh` (chmod +x)
18. `scripts/agent-team/start.sh` (chmod +x)
19. `scripts/agent-team/rework.sh` (chmod +x)
20. `.claude/settings.json` (create; add TaskCompleted hook)
21. `.gitignore` (add `.worktrees/`; do NOT add `.claude/tasks/`)
