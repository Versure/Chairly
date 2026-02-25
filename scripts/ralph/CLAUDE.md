# Ralph Agent Instructions — Chairly

You are an autonomous coding agent working on Chairly, a multi-tenant SaaS platform for salons and barbershops.

## Context

Before starting any work, read these files to understand the project:

1. **Root `CLAUDE.md`** — coding conventions, forbidden patterns, tech stack
2. **`docs/domain-model.md`** — bounded contexts, entities, ubiquitous language
3. **`docs/adr/`** — architecture decision records
4. **The relevant spec in `docs/specs/`** — referenced by the current story

## Your Task

1. Read the PRD at `scripts/ralph/prd.json`
2. Read the progress log at `scripts/ralph/progress.txt` (check Codebase Patterns section first)
3. Check you're on the correct branch from PRD `branchName`. If not, create it from `main`.
4. Pick the **highest priority** user story where `passes: false`
5. Read the relevant spec in `docs/specs/` if referenced
6. Implement that single user story
7. Run quality checks (see below)
8. If checks pass, commit ALL changes with message: `feat({context}): {Story ID} - {Story Title}`
9. Update the PRD to set `passes: true` for the completed story
10. Append your progress to `scripts/ralph/progress.txt`

## Quality Checks

Run these checks after implementing a story. ALL must pass before committing.

### Backend (if you changed files in `src/backend/`)

```bash
dotnet build src/backend/Chairly.slnx
dotnet test src/backend/Chairly.slnx
dotnet format src/backend/Chairly.slnx --verify-no-changes
```

If `dotnet format` fails, fix with `dotnet format src/backend/Chairly.slnx` then verify again.

### Frontend (if you changed files in `src/frontend/`)

```bash
cd src/frontend/chairly
npx nx affected -t lint --base=main
npx nx affected -t test --base=main
npx nx affected -t build --base=main
```

### Both

If you changed files in both backend and frontend, run both sets of checks.

## Implementation Order

Follow this order when implementing a story that spans multiple layers:

1. Domain entities and value objects (`src/backend/Chairly.Domain/`)
2. EF Core configuration and migration (`src/backend/Chairly.Infrastructure/`)
3. Command/Query + Handler + Validator (`src/backend/Chairly.Api/Features/{Context}/{UseCase}/`)
4. API endpoint (`src/backend/Chairly.Api/Features/{Context}/{UseCase}/`)
5. Angular service (`src/frontend/chairly/libs/chairly/src/lib/{domain}/data-access/`)
6. NgRx SignalStore (`src/frontend/chairly/libs/chairly/src/lib/{domain}/data-access/`)
7. UI components (`src/frontend/chairly/libs/chairly/src/lib/{domain}/ui/`)
8. Feature container + routes (`src/frontend/chairly/libs/chairly/src/lib/{domain}/feature/`)
9. Write tests at every step

## Key Conventions

### Backend

- **Vertical Slice Architecture** — each use case is a self-contained slice in `Chairly.Api/Features/{Context}/{UseCase}/`
- **Slice files:** `{UseCase}Command.cs` or `{UseCase}Query.cs`, `{UseCase}Handler.cs`, `{UseCase}Endpoint.cs`, `{UseCase}Validator.cs`
- **Custom mediator** (no MediatR package) — located in `Chairly.Api/Shared/Mediator/`
- **OneOf** for the result pattern — no exceptions for business logic
- **Timestamps over status columns** (ADR-009) — use `{Action}AtUtc` + `{Action}By` pairs
- **Database-per-tenant** — all entities carry `TenantId`
- **File-scoped namespaces**, `var` usage, braces required, `_camelCase` for private fields
- **TreatWarningsAsErrors is ON** — Roslynator + Meziantou analyzers are active, zero warnings allowed
- Naming: `Create{Entity}Command`, `Get{Entity}Query`, `{CommandOrQuery}Handler`, `{CommandOrQuery}Validator`, `{CommandOrQuery}Endpoint`

### Frontend

- **Nx monorepo** at `src/frontend/chairly/` with `@org/chairly-lib` and `@org/shared-lib` path aliases
- **Standalone components**, OnPush change detection, signal-based APIs (`input()`, `model()`, `viewChild()`, `OutputEmitterRef`)
- **No** `@Input()`, `@Output()`, `@ViewChild()` decorators — use signal-based alternatives
- **No** function calls in templates — use signals or pipes
- **NgRx SignalStore** for state management
- **Sheriff** enforces module boundaries — domains cannot import from other domains
- **Component prefix:** `chairly-`
- **Tailwind CSS v4** + **SCSS** for styling
- Strict ESLint: no `any`, no `console`, explicit return types, self-closing tags

### Ubiquitous Language

Always use these terms consistently:
- **Booking** (never "appointment")
- **Client** (never "customer")
- **Staff Member** (never "employee")
- **Service** — a catalog offering
- **Tenant** — a single salon location

## Forbidden

- No `any` types in TypeScript
- No `console` statements in production code
- No business logic in endpoints
- No direct DbContext usage outside Infrastructure layer
- No hardcoded configuration strings
- No status enum columns — use timestamp pairs
- No cross-domain imports in frontend (use `shared/`)
- No MediatR NuGet package
- No `@Input()`/`@Output()`/`@ViewChild()` decorators
- No function calls in Angular templates
- No inline styles in Angular templates
- Never commit without quality checks passing

## Progress Report Format

APPEND to `scripts/ralph/progress.txt` (never replace, always append):

```
## [Date/Time] - [Story ID]
- What was implemented
- Files changed
- **Learnings for future iterations:**
  - Patterns discovered
  - Gotchas encountered
  - Useful context
---
```

## Consolidate Patterns

If you discover a **reusable pattern**, add it to the `## Codebase Patterns` section at the TOP of `scripts/ralph/progress.txt` (create it if it doesn't exist).

## Stop Condition

After completing a user story, check if ALL stories have `passes: true`.

If ALL stories are complete and passing, reply with:
<promise>COMPLETE</promise>

If there are still stories with `passes: false`, end your response normally (another iteration will pick up the next story).

## Important

- Work on ONE story per iteration
- Commit frequently
- Keep quality checks green
- Read the Codebase Patterns section in progress.txt before starting
- Follow existing code patterns — check nearby files for conventions
- When in doubt, choose the simplest approach
