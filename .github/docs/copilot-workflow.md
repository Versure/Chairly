# Copilot CLI Agentic Workflow

This document describes how to use the Copilot CLI agentic workflow to create specifications, implement features, review code, create pull requests, and do rework based on PR comments.

## Prerequisites

- [GitHub Copilot CLI](https://docs.github.com/en/copilot/how-tos/copilot-cli) installed and authenticated
- `gh` CLI authenticated with repo access
- .NET 10 SDK and Node.js 20+ installed
- Repository cloned locally

## Workflow Overview

```
┌──────────────┐     ┌──────────────┐     ┌──────────────────┐
│  1. Spec      │────▶│  2. Review   │────▶│  3. Implement    │
│  (interactive)│     │  (human)     │     │  (autopilot)     │
└──────────────┘     └──────┬───────┘     └────────┬─────────┘
                            │                      │
                     ┌──────▼───────┐     ┌────────▼─────────┐
                     │  2a. Update  │     │  4. Review PR    │
                     │  spec        │     │  (human)         │
                     └──────────────┘     └────────┬─────────┘
                                                   │
                                          ┌────────▼─────────┐
                                          │  4a. Rework      │
                                          │  (agent)         │
                                          └──────────────────┘
```

## Step 1 — Create a Specification

Start an interactive session with the spec-writer agent:

```bash
copilot --agent=spec-writer
```

The agent will:
- Ask clarifying questions about your feature
- Present options when decisions are needed
- Produce a spec at `.github/tasks/{feature-name}/spec.md`

The spec contains:
- **YAML frontmatter** — machine-readable tasks, dependencies, and status tracking
- **Markdown body** — human-readable narrative with user stories, API contracts, UI descriptions

### From a prompt

```bash
copilot --agent=spec-writer --prompt "I want to add a booking management feature where staff members can create, view, and cancel bookings for clients."
```

### From a GitHub issue

```bash
copilot --agent=spec-writer --prompt "Create a spec from issue #42"
```

The agent fetches the issue title, body, and labels via `gh issue view` and uses them as the feature description. The issue link is preserved in the spec for traceability.

## Step 2 — Review the Specification

Open `.github/tasks/{feature-name}/spec.md` and review:
- Are all user stories captured?
- Are the API contracts correct?
- Is the task breakdown reasonable?
- Is anything missing from scope?

### Step 2a — Update the Spec

If you have comments, invoke the spec-writer again:

```bash
copilot --agent=spec-writer --prompt "Update the spec at .github/tasks/booking-management/spec.md: add a user story for recurring bookings and mark the notification integration as out of scope"
```

## Step 3 — Implement the Feature

Once the spec is approved, kick off implementation in autopilot mode:

```bash
copilot --autopilot --yolo --max-autopilot-continues 30 -p "/fleet Implement the feature spec at .github/tasks/{feature-name}/spec.md. Use the backend-dev agent for all backend tasks and the frontend-dev agent for all frontend tasks. Set up git worktrees: .worktrees/{feature-name}/backend/ on branch impl/{feature-name}-backend and .worktrees/{feature-name}/frontend/ on branch impl/{feature-name}-frontend, both branching from feat/{feature-name}. After implementation, use the reviewer agent to review the code. Then merge worktree branches into feat/{feature-name} and create a PR to main."
```

### What happens

1. **Git setup** — Creates `feat/{feature-name}` branch and two worktrees
2. **`/fleet` parallelization** — Delegates backend tasks to `backend-dev` agent and frontend tasks to `frontend-dev` agent, running in parallel
3. **Quality checks** — Each agent runs build/test/lint before committing
4. **Review** — The `reviewer` agent checks the implementation, auto-fixes issues, and writes remaining findings to `review.md`
5. **PR creation** — Merges worktree branches and creates a pull request

### Git branch structure

```
main
 └── feat/{feature-name}              ← PR target
      ├── impl/{feature-name}-backend      ← backend worktree
      └── impl/{feature-name}-frontend     ← frontend worktree
```

## Step 4 — Review the Pull Request

Review the PR on GitHub as usual. Leave inline comments and review summaries.

### Step 4a — Rework Based on PR Comments

After leaving comments, invoke the rework agent:

```bash
copilot --agent=rework --prompt "Fix the review comments on PR #42"
```

The rework agent will:
1. Fetch all PR comments via `gh api`
2. Categorize comments as backend, frontend, or both
3. Apply targeted fixes in the appropriate worktree
4. Run quality checks
5. Commit, push, and post a summary comment on the PR

For a fully autonomous rework pass:

```bash
copilot --autopilot --yolo --agent=rework --prompt "Fix the review comments on PR #42"
```

## Agents Reference

| Agent | File | Purpose | Mode |
|---|---|---|---|
| `spec-writer` | `.github/agents/spec-writer.agent.md` | Create/update feature specs | Interactive |
| `backend-dev` | `.github/agents/backend-dev.agent.md` | Implement backend tasks | Autopilot |
| `frontend-dev` | `.github/agents/frontend-dev.agent.md` | Implement frontend tasks | Autopilot |
| `reviewer` | `.github/agents/reviewer.agent.md` | Review code, auto-fix, report | Autopilot |
| `rework` | `.github/agents/rework.agent.md` | Fix PR review comments | Either |

## Skills Reference

Skills are automatically loaded by Copilot when relevant to the current task.

| Skill | Folder | Loaded when... |
|---|---|---|
| `backend-entity` | `.github/skills/backend-entity/` | Creating domain entities |
| `backend-handler` | `.github/skills/backend-handler/` | Implementing handlers |
| `backend-endpoint` | `.github/skills/backend-endpoint/` | Wiring API endpoints |
| `backend-ef-config` | `.github/skills/backend-ef-config/` | Database configuration/migrations |
| `backend-test` | `.github/skills/backend-test/` | Writing backend tests |
| `frontend-store` | `.github/skills/frontend-store/` | Creating NgRx SignalStores |
| `frontend-service` | `.github/skills/frontend-service/` | Creating API services |
| `frontend-component` | `.github/skills/frontend-component/` | Building Angular components |
| `frontend-routing` | `.github/skills/frontend-routing/` | Setting up routes |
| `frontend-test` | `.github/skills/frontend-test/` | Writing frontend tests |

## Quality Hooks

A pre-commit hook (`.github/hooks/copilot-hooks.json`) automatically runs quality checks before any `git commit`:

- **Backend:** `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`
- **Frontend:** `npx nx affected -t lint test build`, `npx nx format:check`

The hook only runs checks for the layer that has staged changes. If checks fail, the commit is blocked.

## File Structure

```
.github/
├── copilot-instructions.md              # Base project rules (always loaded)
├── instructions/
│   ├── backend.instructions.md          # Backend conventions (src/backend/**)
│   ├── frontend.instructions.md         # Frontend conventions (src/frontend/**)
│   ├── testing.instructions.md          # Test patterns (*.test.*, *.spec.*)
│   └── workflow.instructions.md         # Spec format (.github/tasks/**)
├── agents/
│   ├── spec-writer.agent.md             # Spec creation agent
│   ├── backend-dev.agent.md             # Backend implementation agent
│   ├── frontend-dev.agent.md            # Frontend implementation agent
│   ├── reviewer.agent.md                # Code review agent
│   └── rework.agent.md                  # PR rework agent
├── skills/
│   ├── backend-entity/SKILL.md          # Domain entity patterns
│   ├── backend-handler/SKILL.md         # Handler patterns
│   ├── backend-endpoint/SKILL.md        # Endpoint patterns
│   ├── backend-ef-config/SKILL.md       # EF Core config patterns
│   ├── backend-test/SKILL.md            # Backend test patterns
│   ├── frontend-store/SKILL.md          # NgRx SignalStore patterns
│   ├── frontend-service/SKILL.md        # API service patterns
│   ├── frontend-component/SKILL.md      # Component patterns
│   ├── frontend-routing/SKILL.md        # Route patterns
│   └── frontend-test/SKILL.md           # Frontend test patterns
├── hooks/
│   └── copilot-hooks.json               # Pre-commit quality gate
├── tasks/
│   └── {feature-name}/
│       ├── spec.md                      # Feature specification
│       └── review.md                    # Review findings (generated)
└── docs/
    └── copilot-workflow.md              # This file
```

## Tips

- **Adjust `--max-autopilot-continues`** based on feature size. Small features: 10-15. Large features: 30-50.
- **Check `/skills list`** to see which skills Copilot has loaded.
- **Use `/agent`** in interactive mode to switch between agents mid-session.
- **Review `review.md`** after implementation to see what the reviewer found and couldn't auto-fix.
- If `/fleet` doesn't delegate to custom agents as expected, fall back to sequential invocation:
  ```bash
  copilot --autopilot --yolo --agent=backend-dev --prompt "Implement backend tasks from .github/tasks/{feature}/spec.md in worktree .worktrees/{feature}/backend/"
  copilot --autopilot --yolo --agent=frontend-dev --prompt "Implement frontend tasks from .github/tasks/{feature}/spec.md in worktree .worktrees/{feature}/frontend/"
  copilot --autopilot --yolo --agent=reviewer --prompt "Review the implementation for feature {feature}"
  ```
