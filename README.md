# Chairly

Multi-tenant SaaS platform for salons and barbershops. Built with .NET 10, Angular 21, PostgreSQL, and Claude Code.

## Tech Stack

- **Backend:** .NET 10, ASP.NET Core Web API, EF Core, Vertical Slice Architecture
- **Frontend:** Angular 21, Nx monorepo, NgRx SignalStore, Tailwind CSS v4
- **Database:** PostgreSQL (database-per-tenant)
- **Auth:** Keycloak
- **Infra:** .NET Aspire, Docker, RabbitMQ
- **Testing:** xUnit, Vitest, Playwright
- **AI Workflow:** Claude Code with agent teams

## Prerequisites

- [Claude Code CLI](https://docs.anthropic.com/en/docs/claude-code) (`npm install -g @anthropic-ai/claude-code`)
- [GitHub CLI](https://cli.github.com/) (`gh`) — authenticated
- .NET 10 SDK
- Node.js 20+
- Docker (for local infra)

## Development Workflow

Chairly uses an AI-first development workflow powered by Claude Code. Features are built through slash commands:

### 1. Create a Spec

```bash
# From a description
/create-spec booking-reminders

# From a GitHub issue
/create-spec booking-reminders --issue 42
```

The spec-writer agent (Opus) interviews you interactively:
- Presents decisions with options (bounded context, entity fields, API routes, UI structure)
- Writes `spec.md` + `tasks.json` to `.claude/tasks/{feature}/`
- Spec-reviewer agent (Sonnet) automatically checks completeness and conventions
- Creates a `spec/{feature}` branch with a PR for human review

### 2. Rework a Spec (if needed)

```bash
/rework-spec 45    # PR number
```

Fetches PR review comments, spawns spec-writer to apply targeted fixes, pushes updates.

### 3. Implement a Feature

```bash
/implement booking-reminders
```

After the spec PR is merged to main:
- Creates `feat/{feature}` branch with isolated git worktrees
- Spawns backend + frontend implementation agents (Opus) in parallel
- Spawns backend + frontend reviewer agents (Sonnet) for automated code review
- Spawns QA agents (Sonnet) for build/test/lint validation
- Merges worktrees, creates PR, waits for CI

### 4. Rework Code (if needed)

```bash
/rework-code 48    # PR number
```

Fetches PR review comments, categorizes by layer, spawns fix agents, runs QA, pushes fixes.

### 5. Quick Bug Fix

```bash
/fix manager can't see revenue on dashboard
```

Skips the spec PR cycle for small, well-understood bugs:
- Investigates the codebase to identify root cause
- Creates a `fix/{name}` branch with isolated worktrees
- Spawns fix agents for affected layers (backend/frontend)
- Runs QA, commits, and creates a PR

Use `/fix` for small bugs (1-4 tasks). Use `/create-spec` + `/implement` for larger changes.

### 6. Review Changes

```bash
/review                    # Review current branch
/review feat/my-feature    # Review a specific branch
```

Standalone code review that works outside the `/implement` workflow:
- Detects which layers changed (backend/frontend/infra)
- Spawns appropriate reviewer agents in parallel
- Presents a unified report with findings
- Optionally spawns fix agents to address issues

### 7. Repository Cleanup

```bash
/cleanup
```

Housekeeping tasks:
- Prunes local branches that have been merged into `main`
- Removes orphaned task files with missing specs
- Cleans stale git worktrees

### Workflow Diagram

```
/create-spec ──► spec-writer ──► spec-reviewer ──► spec PR
                                                      │
                                            human review + merge
                                                      │
/implement ───► backend-impl ◄──► backend-reviewer ──┤
              ► frontend-impl ◄──► frontend-reviewer  │
              ► backend-qa + frontend-qa ─────────────┤
                                                      ▼
                                                   code PR
                                                      │
                                            human review + merge
                                                      │
/rework-code ◄────────────────────────────────────────┘

/fix ─────────► backend-impl + frontend-impl ──► QA ──► fix PR
/review ──────► backend-reviewer + frontend-reviewer ──► report
/cleanup ─────► prune branches, tasks, worktrees
```

## Agents

| Agent | Model | Role |
|-------|-------|------|
| spec-writer | Opus | Interactive spec author |
| spec-reviewer | Sonnet | Spec quality reviewer |
| infra-impl | Opus | Infrastructure (Aspire, Keycloak, RabbitMQ, SMTP, seeding) |
| infra-reviewer | Sonnet | Infrastructure code reviewer |
| backend-impl | Opus | .NET backend implementation (VSA slices) |
| frontend-impl | Opus | Angular frontend implementation |
| backend-reviewer | Sonnet | Backend code reviewer |
| frontend-reviewer | Sonnet | Frontend code reviewer |
| chairly-backend-qa | Sonnet | Backend build/test/format |
| chairly-frontend-qa | Sonnet | Frontend lint/test/build/e2e |
| chairly-explorer | Haiku | Read-only codebase lookups |

## Quick Reference

| Command | Description |
|---------|-------------|
| `/create-spec {name} [--issue N]` | Create feature spec interactively |
| `/implement {name}` | Implement a merged spec |
| `/rework-spec {PR#}` | Fix spec from PR review comments |
| `/rework-code {PR#}` | Fix code from PR review comments |
| `/fix {description}` | Quick bug fix without spec cycle |
| `/review [branch]` | Standalone code review |
| `/cleanup` | Prune branches, tasks, and worktrees |

## Quality Checks

**Backend:**
```bash
dotnet build src/backend/Chairly.slnx
dotnet test src/backend/Chairly.slnx
dotnet format src/backend/Chairly.slnx --verify-no-changes
```

**Frontend:**
```bash
cd src/frontend/chairly
npx nx affected -t lint --base=main
npx nx format:check --base=main
npx nx affected -t test --base=main
npx nx affected -t build --base=main
```

## Documentation

- `docs/domain-model.md` — Domain model and ubiquitous language
- `docs/adr/` — Architecture Decision Records
- `.claude/tasks/` — Feature specifications (created by `/create-spec`)
- `docs/ai-workflow.md` — Detailed AI workflow documentation
- `docs/plan.md` — Development plan and feature roadmap
- `CLAUDE.md` — Claude Code conventions and instructions
