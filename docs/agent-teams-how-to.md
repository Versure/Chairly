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

Install these in WSL before first use:

```bash
# tmux — required to run Claude in a detachable session
sudo apt-get install tmux

# gh — GitHub CLI (must be authenticated)
gh auth login

# jq — required by rework.sh
sudo apt-get install jq

# claude — Claude Code CLI
npm install -g @anthropic-ai/claude-code

# dotnet ef (for backend migrations)
dotnet tool install --global dotnet-ef
```

Verify everything works:
```bash
tmux -V && gh auth status && jq --version && claude --version
```

---

## Implementing a feature

### 1. Open WSL and navigate to the project

```bash
wsl
cd ~/projects/Chairly   # or wherever your WSL clone lives
git pull
```

### 2. Run the start script

Pass a short feature description as a single argument:

```bash
./scripts/agent-team/start.sh "Add booking CRUD for clients"
```

Or point to a description file:

```bash
./scripts/agent-team/start.sh /path/to/feature-description.md
```

The script will:
- Derive a kebab-case feature name (e.g. `add-booking-crud`)
- Create a feature branch `feat/add-booking-crud` from `main`
- Create two git worktrees:
  - `.worktrees/backend/` on branch `feat/add-booking-crud/backend`
  - `.worktrees/frontend/` on branch `feat/add-booking-crud/frontend`
- Start a tmux session and launch Claude Code
- Automatically invoke the `/feature-team` skill

### 3. Watch progress (optional)

```bash
tmux attach -t feature-team-add-booking-crud
```

Detach at any time with `Ctrl+B D` — the agents keep running.

The lead agent logs its phase status in every response:
```
[Phase 0 ✓] [Phase 1 ✓] [Phase 2 running...]
```

### 4. Wait for the PR

When all phases complete, a pull request is created on `feat/add-booking-crud` targeting
`main`. The PR URL is printed in the tmux session. You can also find it with:

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

Kill the tmux session to stop the agents at any point:

```bash
tmux kill-session -t feature-team-add-booking-crud
```

Clean up worktrees if you want to start fresh:

```bash
git worktree remove --force .worktrees/backend
git worktree remove --force .worktrees/frontend
git branch -D feat/add-booking-crud/backend feat/add-booking-crud/frontend
```

---

## File layout created per feature

```
.claude/tasks/{feature-name}/
├── spec.md          ← human-readable spec (written by Phase 0)
└── tasks.json       ← machine-readable task list (written by Phase 0)

.worktrees/
├── backend/         ← backend git worktree (feat/{name}/backend)
└── frontend/        ← frontend git worktree (feat/{name}/frontend)
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

**The skill command was not sent to Claude (tmux timing issue)**

The `start.sh` sleeps 6 seconds before sending the `/feature-team` command. If Claude
took longer to start, attach to the session and send it manually:

```bash
tmux attach -t feature-team-{feature-name}
# Then type:
/feature-team "your feature description"
```

**The worktrees already exist from a previous run**

`start.sh` removes stale worktrees automatically. If it fails, remove them manually:

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
| `rework-team` | `/rework-team 42` | Address PR review comments |

Both skills are also triggered automatically by the shell scripts.
