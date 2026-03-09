# Agent Teams — How to Implement Features Autonomously

This guide explains how to use the `feature-team` workflow to implement a feature
end-to-end using a team of Claude Code agents. For the design rationale and architecture,
see [agent-teams-workflow.md](agent-teams-workflow.md).

---

## What this is

The `feature-team` workflow runs a team of Claude Code subagents that take a feature
description, write a spec, implement backend and frontend, review the code, run all
quality checks, and open a pull request — without human intervention.

Use this when you want to hand off an entire feature to agents and review the output as a PR.
Use Ralph (`docs/ai-workflow.md`) when you prefer story-by-story iteration with tighter control.

---

## Prerequisites

Install these before first use:

```bash
# gh — GitHub CLI (must be authenticated)
gh auth login

# jq — required by rework.sh
sudo apt-get install jq   # WSL/Linux
# or: winget install jqlang.jq  # Windows

# dotnet ef (for backend migrations)
dotnet tool install --global dotnet-ef
```

Verify:
```bash
gh auth status && jq --version
```

---

## Implementing a feature

### 1. Open Claude Code on the main branch

```bash
git checkout main
git pull origin main
claude --dangerously-skip-permissions
```

> The `--dangerously-skip-permissions` flag is required so Claude can create branches, worktrees, write files, and run quality checks without prompting at every step. Only use this in the Chairly project directory.

### 2. Run the feature-team skill

Pass a short description or a path to a spec file:

```
/feature-team "Add booking CRUD for clients"
```

```
/feature-team docs/specs/bookings.md
```

The skill will automatically:
- Derive a kebab-case feature name (e.g. `add-booking-crud`)
- Create a feature branch `feat/add-booking-crud` from `main`
- Create two git worktrees:
  - `.worktrees/backend/` on branch `impl/add-booking-crud-backend`
  - `.worktrees/frontend/` on branch `impl/add-booking-crud-frontend`
- Run all phases (spec → backend → frontend → review → QA → merge/PR)

### 3. Follow progress

The lead agent logs its phase status in every response:
```
[Step 0 ✓] [Phase 0 ✓] [Phase 1 ✓] [Phase 2 running...]
```

### 4. Wait for the PR

When all phases complete, a pull request is created on `feat/add-booking-crud` targeting
`main`. The PR URL is printed at the end. You can also find it with:

```bash
gh pr list
```

### 5. Review the PR on GitHub

The PR description lists all implemented tasks, quality gate results, and any known
issues the agents could not resolve.

---

## Reviewing and requesting changes

After reviewing the PR on GitHub, leave inline comments or review summaries directly
in the GitHub UI as you normally would.

Once you are done reviewing, trigger the rework workflow from WSL:

```bash
./scripts/agent-team/rework.sh 42   # replace 42 with your PR number
```

The script will:
- Fetch all your review comments from the PR
- Re-create the worktrees from the feature branch
- Start a new tmux session and launch Claude Code
- Automatically invoke the `/rework-team` skill

The agents read your comments, categorize them as backend/frontend fixes, implement
the changes in parallel, run all quality checks, and push a new commit to the PR branch.
A summary comment is posted on the PR when done.

Repeat this cycle until you are satisfied, then merge normally.

---

## Aborting a run

Press `Ctrl+C` or close Claude Code to stop the agents at any point.

Clean up worktrees if you want to start fresh:

```bash
git worktree remove --force .worktrees/backend
git worktree remove --force .worktrees/frontend
git branch -D impl/add-booking-crud-backend impl/add-booking-crud-frontend
```

---

## File layout created per feature

```
.claude/tasks/{feature-name}/
├── spec.md          ← human-readable spec (written by Phase 0)
└── tasks.json       ← machine-readable task list (written by Phase 0)

.worktrees/
├── backend/         ← backend git worktree (impl/{name}-backend)
└── frontend/        ← frontend git worktree (impl/{name}-frontend)
```

The spec and tasks files are committed to the feature branch at the end of Phase 5
so they are visible in the PR.

---

## Workflow phases

| Phase | What happens |
|---|---|
| 0 — Spec | Agent writes `spec.md` and `tasks.json` based on your description |
| 1 — Backend | Agent implements all backend tasks in `.worktrees/backend/` |
| 2 — Frontend | Agent implements all frontend tasks in `.worktrees/frontend/` |
| 3 — Review | Two reviewer agents (backend + frontend) run in parallel; fix agents address findings |
| 4 — QA | `chairly-backend-qa` and `chairly-frontend-qa` run full quality checks; auto-fix where possible |
| 5 — Merge | Worktrees merged into feature branch, PR created |

---

## Troubleshooting

**The skill reports "must be run from the main branch"**

```bash
git checkout main && git pull origin main
```

Then re-run `/feature-team`.

**The worktrees already exist from a previous run**

The skill removes stale worktrees automatically. If it fails, remove them manually:

```bash
git worktree remove --force .worktrees/backend
git worktree remove --force .worktrees/frontend
```

**A quality check failed and the PR notes it as a known issue**

Describe the failure in your PR review comment. The rework workflow will attempt to fix it.

**The rework script cannot find the feature branch**

Ensure the PR's head branch starts with `feat/`. Only PRs created by `feature-team` are
supported by `rework.sh`.

**The TaskCompleted hook is not firing**

The hook fires when agents use Claude Code's native task system. If it is not working,
run the quality checks manually in the worktree:

```bash
# Backend
cd .worktrees/backend && dotnet build src/backend/Chairly.slnx
# Frontend
cd .worktrees/frontend/src/frontend/chairly && npx nx affected -t build --base=main
```

---

## Skill reference

| Skill | Invocation | Purpose |
|---|---|---|
| `feature-team` | `/feature-team "description"` | Start full feature implementation |
| `rework-team` | triggered by `rework.sh` | Address PR review comments |
