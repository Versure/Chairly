# CLAUDE.md

## Project

Chairly — Multi-tenant SaaS platform for salons and barbershops.

## Architecture

- Read `docs/domain-model.md` for the domain model and ubiquitous language
- Read `docs/adr/` for architecture decisions
- Feature specs are in `docs/specs/`
- AI workflow docs in `docs/ai-workflow.md`

## Tech Stack

- **Backend:** .NET 10, ASP.NET Core Web API, EF Core, OneOf (result pattern)
- **Frontend:** Angular 21, Nx monorepo, NgRx SignalStore, Tailwind CSS v4, SCSS, Sheriff
- **Testing:** xUnit (backend), Vitest (frontend unit), Playwright (frontend e2e)
- **Infra:** PostgreSQL (database-per-tenant), RabbitMQ, Keycloak, .NET Aspire, Docker

## Backend Architecture — Vertical Slice Architecture

The backend uses a hybrid VSA approach with thin shared layers.

**Projects:**
- `Chairly.Domain` — Entities, value objects, enums. No use-case logic. No EF Core dependency.
- `Chairly.Infrastructure` — DbContext, EF configurations, RabbitMQ, Keycloak integration.
- `Chairly.Api` — Vertical slices organized by feature. Contains the custom mediator.
- `Chairly.AppHost` — .NET Aspire orchestrator for local development.
- `Chairly.ServiceDefaults` — Shared Aspire configuration.
- `Chairly.Tests` — Unit and integration tests.

**Slice structure:**
```
Chairly.Api/Features/{Context}/{UseCase}/
  ├── {UseCase}Command.cs or {UseCase}Query.cs
  ├── {UseCase}Handler.cs
  ├── {UseCase}Endpoint.cs
  └── {UseCase}Validator.cs
```

**Rules:**
- Each slice contains everything for one use case: command/query, handler, validator, endpoint
- Slices in the same bounded context may reference each other
- Slices across bounded contexts must NOT reference each other — use domain events or shared contracts
- Business logic lives in handlers, NEVER in endpoints

## Custom Mediator (ADR-005)

We use a custom mediator implementation (MediatR pattern, no MediatR package). Located in `Chairly.Api/Shared/Mediator/`.

Interfaces: `IRequest<TResponse>`, `IRequestHandler<TRequest, TResponse>`, `IMediator`, `IPipelineBehavior<TRequest, TResponse>`

## Code Conventions — Backend

**Naming:**
- Commands: `Create{Entity}Command`, `Update{Entity}Command`, `Delete{Entity}Command`
- Queries: `Get{Entity}Query`, `Get{Entities}ListQuery`
- Handlers: `{CommandOrQuery}Handler`
- Validators: `{CommandOrQuery}Validator`
- Endpoints: `{CommandOrQuery}Endpoint`

**Patterns:**
- Use OneOf for the result pattern (no exceptions for business logic)
- Data Annotations + manual validation for input validation
- Entity configurations in separate `IEntityTypeConfiguration<T>` classes
- All endpoints via Minimal APIs, grouped per feature in extension methods
- Timestamps instead of status columns (see ADR-009):
  - `{Action}AtUtc` + `{Action}By` pairs (e.g. `ConfirmedAtUtc`, `ConfirmedBy`)
  - Status is derived from timestamps, never stored
  - `CreatedAtUtc`/`CreatedBy` required on all entities
- Database-per-tenant: all entities carry `TenantId`, tenant resolution via middleware
- Test coverage: unit tests for handlers, integration tests for API endpoints

## Frontend Architecture — Nx Monorepo with DDD & Sheriff (ADR-006)

**Workspace:** `src/frontend/chairly/` — Nx monorepo with apps/libs layout.

**Structure:**
```
src/frontend/chairly/
├── apps/
│   ├── chairly/              # Main Angular application
│   └── chairly-e2e/          # Playwright e2e tests
├── libs/
│   ├── chairly/src/lib/      # Domain library
│   │   ├── bookings/         # Each domain has feature/, ui/, data-access/, util/
│   │   ├── clients/
│   │   ├── staff/
│   │   ├── services/
│   │   ├── billing/
│   │   └── notifications/
│   └── shared/src/lib/       # Shared library
│       ├── ui/               # Shared components (buttons, modals)
│       ├── data-access/      # Auth, HTTP interceptors
│       └── util/             # Shared helpers
├── eslint.config.mjs         # Root ESLint config (12 plugins)
├── sheriff.config.ts         # Module boundary rules
└── postcss.config.mjs        # Tailwind v4
```

**Path aliases:** `@org/chairly-lib`, `@org/shared-lib`

**Module boundary rules (enforced by Sheriff):**
- A domain cannot import from another domain
- A domain can import from `shared/`
- `shared/` cannot import from any domain
- Within a domain: `feature/` → `ui/`, `data-access/`, `util/`. `ui/` → `util/` only. `data-access/` → `util/` only.

## Code Conventions — Frontend

- Standalone components, no NgModules
- **OnPush change detection** on all components
- Angular signals over decorators: `input()` over `@Input()`, `model()` over `@Input`+`@Output`, `OutputEmitterRef` over `EventEmitter`
- NgRx SignalStore for shared/feature state
- Smart/Dumb component pattern: containers load data, presentational components display it
- Services for API calls, one service per backend context
- Tailwind CSS v4 for styling, SCSS for component styles
- Reactive forms with typed FormGroups
- Lazy-loaded routes per domain
- Component prefix: `chairly-` (e.g. `<chairly-booking-list>`)
- No `any` types in TypeScript
- No `console` statements (use proper logging)
- Explicit return types on all functions
- Self-closing tags for empty elements (e.g. `<chairly-icon />`)
- No function calls in templates — use signals/pipes instead
- Imports auto-sorted: Angular → third-party → project aliases → relative

## Ubiquitous Language

- **Booking** (never "appointment") — a scheduled visit with one or more services
- **Client** (never "customer") — a person who receives services
- **Staff Member** (never "employee") — a person who works at the salon
- **Service** — a catalog offering (e.g. "Men's Haircut")
- **Tenant** — a single salon location

See `docs/domain-model.md` for the complete glossary.

## User Roles

- **Owner** — full admin, one per tenant
- **Manager** — manages staff and schedules, no billing/settings access
- **Staff Member** — sees and manages own schedule only

## Working Method — Interactive Mode

When working with a human developer interactively:
- STOP and ask questions when something is not described in a spec, ADR, or previous instruction
- Provide 2-3 concrete options with pros and cons
- Wait for the human's choice before proceeding
- You may NEVER make assumptions about technical choices

## Working Method — Autonomous/Headless Mode (Ralph)

When running autonomously via Ralph or in headless mode:
- Do NOT stop to ask questions — there is no one to answer
- Make decisions based on existing patterns in the codebase, ADRs, and specs
- Follow conventions established in docs/ and existing code
- Document any significant decisions in progress.txt
- When in doubt, choose the simplest approach that follows existing patterns
- **IMPORTANT — When ALL stories are complete (all `passes: true`), you MUST push the branch and create a PR BEFORE outputting the `<promise>COMPLETE</promise>` signal:**
  ```bash
  git push -u origin HEAD
  gh pr create --title "feat({context}): {feature description}" --body "Implemented by Ralph. See prd.json for stories."
  ```
  Replace `{context}` with the bounded context (e.g. bookings, staff) and `{feature description}` with a short summary from the PRD.

## Implementation Order

1. Always read the relevant spec in `docs/specs/` before starting
2. Create domain entities and value objects first
3. Then EF Core configuration and migration
4. Then handlers with validation
5. Then API endpoints
6. Then Angular feature (service → store → components → routes)
7. Write tests at every step
8. Commit with conventional commits: `feat(bookings): add create booking endpoint`

## Quality Checks

Run these checks before committing. ALL must pass.

**Backend** (if you changed files in `src/backend/`):
```bash
dotnet build src/backend/Chairly.slnx
dotnet test src/backend/Chairly.slnx
dotnet format src/backend/Chairly.slnx --verify-no-changes
```
If `dotnet format` fails, auto-fix with `dotnet format src/backend/Chairly.slnx` then verify again.

**Frontend** (if you changed files in `src/frontend/`):
```bash
cd src/frontend/chairly
npx nx affected -t lint --base=main
npx nx affected -t test --base=main
npx nx affected -t build --base=main
```

## Forbidden

- No `any` types in TypeScript
- No `console` statements in production code
- No business logic in controllers/endpoints
- No direct use of DbContext outside Infrastructure layer
- No hardcoded strings for configuration
- No status enum columns — use timestamp pairs (ADR-009)
- No cross-domain imports in the frontend without going through `shared/`
- No MediatR NuGet package — use the custom mediator
- No `@Input()`/`@Output()`/`@ViewChild()` decorators — use signal-based APIs (`input()`, `model()`, `viewChild()`, `OutputEmitterRef`)
- No function calls in Angular templates — use signals or pipes
- No inline styles in Angular templates
- Never commit without tests passing
