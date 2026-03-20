# AI Workflow — Claude Code Agent Team

This document describes the development workflow for Chairly using Claude Code as the primary development tool. For the decision rationale, see [ADR-010](adr/ADR-010-ai-first-development.md).

---

## Overview

Chairly uses a structured AI-first workflow built on Claude Code skills and agents. The workflow has five steps: spec creation, spec review, implementation, code review, and rework cycles.

All orchestration happens through four slash commands:
- `/create-spec` — write and review a feature spec, create a PR
- `/implement` — implement a spec with parallel backend/frontend agents
- `/rework-spec` — fix a spec based on PR review comments
- `/rework-code` — fix code based on PR review comments

---

## Workflow Diagram

```
                    ┌──────────────────┐
                    │  /create-spec    │
                    │  {name} [--issue]│
                    └────────┬─────────┘
                             │
              ┌──────────────┼──────────────┐
              ▼              ▼              │
     ┌─────────────┐ ┌──────────────┐      │
     │ spec-writer  │ │ spec-reviewer│      │
     │ (Opus)       │ │ (Sonnet)     │      │
     └──────┬──────┘ └──────┬───────┘      │
            │               │              │
            ▼               ▼              │
     ┌──────────────────────────┐          │
     │ spec/{name} branch + PR  │          │
     └────────────┬─────────────┘          │
                  │                        │
         ┌────── ▼ ──────┐                │
         │ Human reviews  │◄───────────────┘
         │ spec PR        │     /rework-spec
         └────────┬───────┘
                  │ merge
                  ▼
     ┌────────────────────────┐
     │     /implement         │
     │     {feature-name}     │
     └────────────┬───────────┘
                  │
    ┌─────────────┼─────────────┐
    ▼                           ▼
┌──────────┐            ┌──────────────┐
│ backend  │            │ frontend     │
│ -impl    │            │ -impl        │
│ (Opus)   │            │ (Opus)       │
│ worktree │            │ worktree     │
└────┬─────┘            └──────┬───────┘
     │                         │
     ├── backend-reviewer ◄────┤── frontend-reviewer
     ├── backend-qa       ◄────┤── frontend-qa
     │                         │
     └─────────┬───────────────┘
               │ merge
               ▼
     ┌──────────────────┐
     │ feat/{name} PR   │
     └────────┬─────────┘
              │
     ┌────── ▼ ──────┐
     │ Human reviews  │◄──── /rework-code
     │ code PR        │
     └────────┬───────┘
              │ merge
              ▼
           main
```

---

## Step 1-3: Spec Creation (`/create-spec`)

```bash
/create-spec booking-reminders --issue 42
```

1. Parses arguments (feature name, optional GitHub issue number)
2. If `--issue` provided, fetches issue content via `gh issue view`
3. Spawns **spec-writer** agent (Opus, interactive):
   - Reads domain model and existing specs
   - Presents decisions to the user (bounded context, entity fields, API routes, UI structure)
   - Writes `spec.md` + `tasks.json` to `.claude/tasks/{feature}/`
4. Spawns **spec-reviewer** agent (Sonnet, read-only):
   - Checks completeness, domain consistency, conventions
   - Returns structured pass/fail with findings
5. If issues found, user decides whether to fix — spec-writer re-runs with findings
6. Creates `spec/{feature}` branch, commits, pushes, creates PR

---

## Step 3a: Spec Rework (`/rework-spec`)

```bash
/rework-spec 45
```

1. Fetches PR review comments via `gh` CLI
2. Checks out spec branch
3. Spawns **spec-writer** agent with existing spec + review comments
4. Agent modifies spec based on comments (not full rewrite)
5. Commits, pushes, replies to PR with summary

---

## Step 4-5: Implementation (`/implement`)

```bash
/implement booking-reminders
```

1. Verifies spec exists on main (must merge spec PR first)
2. Creates `feat/{feature}` branch from main
3. Creates two git worktrees: backend and frontend
4. **Phase 0.5** (if infra tasks exist) — Spawns sequentially:
   - **infra-impl** agent (Opus) in backend worktree — Aspire, Keycloak, RabbitMQ, SMTP, seeding
   - **infra-reviewer** agent (Sonnet, read-only) — reviews infra changes
5. **Phase 1** — Spawns in parallel:
   - **backend-impl** agent (Opus) in backend worktree
   - **frontend-impl** agent (Opus) in frontend worktree
6. **Phase 2** — Spawns in parallel:
   - **backend-reviewer** agent (Sonnet, read-only)
   - **frontend-reviewer** agent (Sonnet, read-only)
   - If issues found: fix agents run, then one re-review pass
7. **Phase 3** — Spawns in parallel:
   - **chairly-backend-qa** agent (Sonnet) — build, test, format
   - **chairly-frontend-qa** agent (Sonnet) — lint, format, test, build, e2e
   - If QA fails: fix and retry up to 2 times
8. **Phase 4** — Merges worktrees, creates PR, waits for CI

---

## Step 5a: Code Rework (`/rework-code`)

```bash
/rework-code 48
```

1. Fetches PR review comments via `gh` CLI
2. Categorizes comments as backend/frontend
3. Recreates worktrees on the feature branch
4. Spawns fix agents in parallel (backend + frontend) with comment context
5. Runs QA agents (retry up to 2 times)
6. Merges worktrees, pushes, replies to PR with summary

---

## Agents

| Agent | Model | Tools | Role |
|-------|-------|-------|------|
| spec-writer | Opus | Full + Agent | Writes specs interactively |
| spec-reviewer | Sonnet | Read-only | Reviews specs |
| infra-impl | Opus | Full | Implements infrastructure (Aspire, Keycloak, RabbitMQ, SMTP, seeding) |
| infra-reviewer | Sonnet | Read-only | Reviews infrastructure code |
| backend-impl | Opus | Full | Implements backend VSA slices |
| frontend-impl | Opus | Full | Implements frontend domains |
| backend-reviewer | Sonnet | Read-only | Reviews backend code |
| frontend-reviewer | Sonnet | Read-only | Reviews frontend code |
| chairly-backend-qa | Sonnet | Full | Runs backend quality checks |
| chairly-frontend-qa | Sonnet | Full | Runs frontend quality checks |
| chairly-explorer | Haiku | Read-only | Lightweight codebase lookups |

---

## Quality Checks

**Backend** (run by chairly-backend-qa):
- `dotnet build` — compilation
- `dotnet test` — unit + integration tests
- `dotnet format --verify-no-changes` — formatting

**Frontend** (run by chairly-frontend-qa):
- `nx lint` — ESLint
- `nx format:check` — Prettier
- `nx test` — unit tests
- `nx build` — compilation
- `nx run chairly-e2e:e2e` — Playwright e2e

---

## File Structure

```
.claude/
├── agents/                    # Agent definitions
│   ├── spec-writer.md
│   ├── spec-reviewer.md
│   ├── infra-impl.md
│   ├── infra-reviewer.md
│   ├── backend-impl.md
│   ├── frontend-impl.md
│   ├── backend-reviewer.md
│   ├── frontend-reviewer.md
│   ├── chairly-backend-qa.md
│   ├── chairly-frontend-qa.md
│   └── chairly-explorer.md
├── skills/                    # Skill orchestrators + phase files
│   ├── create-spec/
│   ├── implement/
│   ├── rework-spec/
│   ├── rework-code/
│   ├── chairly-backend-slice/ # Pattern reference
│   ├── chairly-frontend-domain/ # Pattern reference
│   └── chairly-spec-format/   # Spec format reference
└── tasks/                     # Feature specs and task lists
    └── {feature-name}/
        ├── spec.md
        └── tasks.json
```

---

## Definition of Done (Per Feature)

- [ ] Spec reviewed and merged to main
- [ ] All implementation tasks completed
- [ ] Backend QA passes (build, test, format)
- [ ] Frontend QA passes (lint, format, test, build, e2e)
- [ ] Code reviewed by human
- [ ] CI green
- [ ] Feature branch merged to main
