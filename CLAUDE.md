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

**Async patterns in Program.cs (analyzer rules):**
- Avoid `await using` for `IAsyncDisposable` scopes — use try/finally with `await scope.DisposeAsync().ConfigureAwait(false)` instead (required by CA2007/MA0004)
- As soon as any `await` is introduced in Program.cs, change `app.Run()` to `await app.RunAsync().ConfigureAwait(false)` (required by CA1849)

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
│   │   ├── bookings/         # Each domain has the layers below
│   │   ├── clients/          #   feature/{feature-name}/  ← one subfolder per smart component
│   │   ├── staff/            #   ui/                      ← presentational components
│   │   ├── services/         #   data-access/             ← stores, API services
│   │   ├── billing/          #   models/                  ← TypeScript interfaces/types
│   │   └── notifications/    #   pipes/                   ← Angular pipes
│   │                         #   util/                    ← pure utility functions
│   │                         #   {domain}.routes.ts       ← route config at domain root
│   └── shared/src/lib/       # Shared library
│       ├── ui/               # Shared components (buttons, modals)
│       ├── data-access/      # Auth, HTTP interceptors
│       └── util/             # Shared helpers
├── eslint.config.mjs         # Root ESLint config (12 plugins)
├── sheriff.config.ts         # Module boundary rules
├── postcss.config.json       # Tailwind v4 — PostCSS config for @angular/build (JSON only)
└── postcss.config.mjs        # Tailwind v4 — PostCSS config for Vite/Vitest (ESM)
```

**Global styles setup (two separate files — never merge):**
- `apps/chairly/src/tailwind.css` — `@import 'tailwindcss'` + `@source` directives. Processed by PostCSS only. Never goes through Sass.
- `apps/chairly/src/styles.scss` — SCSS-specific global styles: custom variables, mixins, font declarations. Must never contain `@import` for CSS libraries.
- Both are listed in `project.json` build.options.styles: `tailwind.css` first, then `styles.scss`.

**PostCSS config — important constraint:**
- `@angular/build:application` only reads `postcss.config.json` or `.postcssrc.json` (JSON format). It ignores `.js`/`.mjs` configs entirely.
- `postcss.config.json` is the config used by the Angular builder.
- `postcss.config.mjs` is kept for Vite-based tooling (Vitest, Storybook).
- The `tailwind.css` file must include explicit `@source` directives pointing at templates, because Tailwind's auto-detection does not reliably traverse Nx monorepo lib folders. Paths are relative to `tailwind.css`.

**Path aliases:** `@org/chairly-lib`, `@org/shared-lib`

**Module boundary rules (enforced by Sheriff):**
- A domain cannot import from another domain
- A domain can import from `shared/`
- `shared/` cannot import from any domain
- Within a domain: `feature/` → `ui/`, `data-access/`, `models/`, `pipes/`, `util/`. `ui/` → `models/`, `pipes/`, `util/` only. `data-access/` → `models/`, `util/` only.

**Domain folder conventions — checklist (verify before creating any file):**

| File type | Correct folder | Common mistake |
|---|---|---|
| TypeScript interfaces/DTOs | `models/` | Putting them in `util/` |
| Angular `@Pipe` classes | `pipes/` | Putting them in `util/` |
| Pure TS utility functions | `util/` | No interfaces or pipes here |
| Smart (container) components | `feature/{feature-name}/` subfolder | Placing files directly in `feature/` |
| Route configuration | `{domain}.routes.ts` at **domain root** | Placing it inside `feature/` |
| `.gitkeep` | Delete immediately when real files are added | Leaving it after the folder is populated |

## Code Conventions — Frontend

- Standalone components, no NgModules
- **OnPush change detection** on all components
- Angular signals over decorators: `input()` over `@Input()`, `model()` over `@Input`+`@Output`, `OutputEmitterRef` over `EventEmitter`
- NgRx SignalStore for shared/feature state
- Use `takeUntilDestroyed(destroyRef)` from `@angular/core/rxjs-interop` for subscription cleanup — always inject `DestroyRef` and pass it explicitly, even inside a constructor where the argument is optional. Never use the `Subject` + `ngOnDestroy` teardown pattern.
- Smart/Dumb component pattern: containers load data, presentational components display it
- Services for API calls, one service per backend context
- Tailwind CSS v4 for styling, SCSS for component styles. Tailwind v4 is imported in `apps/chairly/src/tailwind.css` (plain CSS). SCSS global styles go in `apps/chairly/src/styles.scss`. These two files must never be merged.
- Reactive forms with typed FormGroups
- Lazy-loaded routes per domain
- Each domain layer has dedicated subfolders: `models/` for interfaces, `pipes/` for Angular pipes, `util/` for pure functions. Routes file (`{domain}.routes.ts`) lives at the **domain root**, not inside `feature/`. Each smart component inside `feature/` has its own subfolder.
- Component prefix: `chairly-` (e.g. `<chairly-booking-list>`)
- **UI language is Dutch (Nederlands) — write Dutch from the first keystroke.** Do not write English UI first and translate later. Every label, button, placeholder, header, validation message, empty state, loading indicator, and dialog title must be Dutch when first written. Common translations: Save→Opslaan, Cancel→Annuleren, Add→Toevoegen, Edit→Bewerken, Delete→Verwijderen, Active→Actief, Inactive→Inactief, Loading→Laden, Confirm→Bevestigen.
- Register Dutch locale in `app.config.ts`: call `registerLocaleData(localeNl)`, and provide both `{ provide: LOCALE_ID, useValue: 'nl-NL' }` and `{ provide: DEFAULT_CURRENCY_CODE, useValue: 'EUR' }` — `LOCALE_ID` alone does not change the currency default of `CurrencyPipe`.
- No `any` types in TypeScript
- No `console` statements (use proper logging)
- Explicit return types on all functions
- Self-closing tags for empty elements (e.g. `<chairly-icon />`)
- No function calls in templates — use signals/pipes instead
- Imports auto-sorted: Angular → third-party → project aliases → relative
- **Templates must always be in a separate `.html` file** using `templateUrl:`. Inline `template:` is forbidden and enforced by ESLint (`@angular-eslint/component-max-inline-declarations: { template: 0 }`)
- **E2E tests with Playwright** must be written for all pages/features whenever possible
- Add E2E tests to `apps/chairly-e2e/src/` for each feature page

## Dark Mode Convention

The dark theme is activated by `data-theme="dark"` on the `<html>` element (managed by `ThemeService`).

- Global CSS overrides in `tailwind.css` cover standard Tailwind classes: `bg-white`, `bg-gray-50`, `text-gray-*`, `border-gray-*`, form inputs.
- **Custom/brand color classes (`bg-primary-*`, `bg-accent-*`) have NO global dark override.** Always add an explicit `dark:` Tailwind variant in the template when using these classes (e.g. `bg-primary-50 dark:bg-slate-800`).
- When adding any light-mode background color to a component, always pair it with a `dark:` variant. Missing a `dark:` on a custom color will cause a light block in dark mode.

## Native `<dialog>` Pattern

Always implement `<dialog>` as a full-screen overlay using `showModal()`:

```html
<dialog
  class="fixed inset-0 m-0 w-screen h-screen max-w-none max-h-none flex items-center justify-center border-0 bg-black/50 p-4"
>
  <div class="bg-white dark:bg-slate-800 rounded-lg shadow-xl w-full max-w-md mx-auto">
    <!-- content -->
  </div>
</dialog>
```

- Inject `DOCUMENT` and set `document.body.style.overflow = 'hidden'` when opening, `''` when closing.
- Use `[open]` CSS attribute selector (or `pointer-events: none` when closed) to prevent closed dialogs from intercepting pointer events.
- To close cross-browser reliably in Playwright e2e tests, use `page.keyboard.press('Escape')` rather than clicking the Cancel button inside a `showModal()` dialog (unreliable in Firefox/WebKit with zoneless Angular).

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
- **IMPORTANT — When ALL stories are complete (all `passes: true`), you MUST push, create a PR, wait for CI, and only then output the `<promise>COMPLETE</promise>` signal:**
  ```bash
  git push -u origin HEAD
  gh pr create --title "feat({context}): {feature description}" --body "Implemented by Ralph. See prd.json for stories."
  # Wait for CI and exit non-zero if it fails:
  gh run watch --exit-status
  ```
  Replace `{context}` with the bounded context (e.g. bookings, staff) and `{feature description}` with a short summary from the PRD.
  If `gh run watch` exits with a failure:
  1. Run `gh run view --log-failed` to read the failure details
  2. Fix the issue, commit the fix, push again
  3. Run `gh run watch --exit-status` again
  4. Only signal `<promise>COMPLETE</promise>` when CI is green

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
npx nx format:check --base=main
npx nx affected -t test --base=main
npx nx affected -t build --base=main
```
If `npx nx format:check` fails, auto-fix with `npx nx format --base=main` then verify again.

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
- No `Subject` + `ngOnDestroy` for subscription cleanup — use `takeUntilDestroyed(destroyRef)` with an injected `DestroyRef`
- No function calls in Angular templates — use signals or pipes
- No inline styles in Angular templates
- No inline `template:` in Angular components — always use `templateUrl:` with a separate `.html` file
- No model interfaces or Angular pipes inside `util/` — use `models/` and `pipes/` folders respectively
- No `@import` of CSS libraries (e.g. Tailwind) inside `.scss` files — use a separate plain `.css` entry file instead
- No PostCSS config in `.js`/`.mjs` format only — always maintain `postcss.config.json` for the Angular builder (it does not read `.mjs`)
- No English user-facing text in the UI — all labels, buttons, messages, and UI copy must be in Dutch (Nederlands)
- No `imports: []` in `@Component` decorators — omit the property entirely when a component has no imports
- Never commit without tests passing
