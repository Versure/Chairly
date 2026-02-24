# ADR-006: Angular 21 Frontend — DDD, Sheriff, Signals, and Testing

## Status

Accepted (updated 2026-02-24)

## Context

We need a frontend framework for the salon management dashboard. The team has Angular experience, and the application requires complex state management (bookings calendar, real-time updates, multi-step forms). We want the frontend to follow Domain-Driven Design principles with strict module boundaries, mirroring the backend's bounded contexts.

## Decision

### Framework & Conventions

We use **Angular 21** with:
- **Standalone components** — no NgModules
- **Angular signals** for reactive local state (enforced via `prefer-signals`, `prefer-signal-model`, `prefer-output-emitter-ref`)
- **NgRx SignalStore** for shared/feature state management
- **Tailwind CSS v4** for styling with **SCSS** for component styles
- **OnPush change detection** on all components (enforced via ESLint)
- **Smart/Dumb component pattern**: container components load data, presentational components display it
- **Reactive forms** with typed FormGroups
- **Lazy-loaded routes** per domain
- **Component prefix**: `chairly-` (enforced via ESLint)

### Workspace Structure

The frontend is an **Nx monorepo** at `src/frontend/chairly/` with an apps/libs layout:

```
src/frontend/chairly/
├── apps/
│   ├── chairly/              # Main Angular application
│   │   ├── src/
│   │   │   ├── app/          # App root component, routes, config
│   │   │   ├── main.ts
│   │   │   ├── styles.scss   # Global styles (@import 'tailwindcss')
│   │   │   └── index.html
│   │   ├── project.json
│   │   └── eslint.config.mjs
│   └── chairly-e2e/          # Playwright e2e tests
│       ├── src/
│       ├── playwright.config.ts
│       └── project.json
├── libs/
│   ├── chairly/              # Domain library
│   │   └── src/lib/
│   │       ├── bookings/     # Each domain has feature/, ui/, data-access/, util/
│   │       ├── clients/
│   │       ├── staff/
│   │       ├── services/
│   │       ├── billing/
│   │       └── notifications/
│   └── shared/               # Shared library
│       └── src/lib/
│           ├── ui/           # Shared presentational components (buttons, modals)
│           ├── data-access/  # Auth service, HTTP interceptors, tenant context
│           └── util/         # Shared helpers, validators, constants
├── eslint.config.mjs         # Root ESLint config with all plugins
├── sheriff.config.ts         # Module boundary rules
├── postcss.config.mjs        # Tailwind v4 PostCSS config
├── .prettierrc               # Formatting rules
├── .editorconfig             # Editor settings
├── .gitattributes            # LF line endings
├── nx.json
├── package.json
└── tsconfig.base.json
```

**Path aliases** (in `tsconfig.base.json`):
- `@org/chairly-lib` → `libs/chairly/src/index.ts`
- `@org/shared-lib` → `libs/shared/src/index.ts`

### Domain-Driven Design Structure

Each domain folder inside `libs/chairly/src/lib/` mirrors the backend bounded contexts 1:1:

- `feature/` — Smart (container) components that inject services and manage state. These are the route-level components.
- `ui/` — Presentational (dumb) components that receive data via `input()` signals and emit events via `OutputEmitterRef`. No service injection.
- `data-access/` — API services (one per domain), NgRx SignalStore definitions, DTOs/models.
- `util/` — Pure helper functions, pipes, mappers. No side effects.

### Module Boundary Enforcement with Sheriff

We use **Sheriff** (`@softarc/sheriff-core` + `@softarc/eslint-plugin-sheriff`) to enforce module boundaries via ESLint.

**Rules (defined in `sheriff.config.ts`):**
- A domain **cannot** import from another domain directly (e.g. `bookings/` cannot import from `billing/`)
- A domain **can** import from `shared/`
- `shared/` cannot import from any domain
- Within a domain: `feature/` → `ui/`, `data-access/`, `util/`. `ui/` → `util/` only. `data-access/` → `util/` only.

### Linting & Formatting

**ESLint plugins (12 total):**
- `@nx/eslint-plugin` — Nx module boundaries
- `@softarc/eslint-plugin-sheriff` — DDD module boundaries
- `angular-eslint` — TS recommended, template recommended, template accessibility
- `@ngrx/eslint-plugin` — SignalStore rules
- `@smarttools/eslint-plugin-rxjs` — RxJS best practices (type-checked)
- `eslint-plugin-rxjs-angular-x` — Angular RxJS rules (takeUntilDestroyed, async pipe)
- `@vitest/eslint-plugin` — Test quality rules
- `eslint-plugin-playwright` — E2E test quality rules
- `eslint-plugin-simple-import-sort` — Auto-sorted/grouped imports
- `eslint-plugin-unused-imports` — Auto-remove unused imports
- `eslint-plugin-sonarjs` — Code smell detection

**Key enforced rules:**
- `chairly-` component/directive prefix
- OnPush change detection on all components
- Angular signals over decorators (`input()` over `@Input()`, etc.)
- Explicit return types on all functions
- No `any` types, no `console` statements
- Strict equality (`===`)
- Max 300 lines per file (warning)
- Self-closing tags, button-has-type, no inline styles, no template function calls

**Prettier:** single quotes, semicolons, 100 char width, trailing commas, LF line endings

**Line endings:** LF everywhere, enforced via `.gitattributes`, `.editorconfig`, and Prettier

### Testing

- **Unit tests:** **Vitest** with `@analogjs/vitest-angular` — fast, ESM-native test runner. Used for component tests, service tests, store tests, and utility tests.
- **E2E tests:** **Playwright** — cross-browser end-to-end testing in `apps/chairly-e2e/`.

## Consequences

- **Positive:** DDD structure makes the frontend predictable — developers know exactly where to find and place code.
- **Positive:** Sheriff enforces boundaries at build time — prevents accidental coupling between domains.
- **Positive:** 1:1 mapping with backend contexts reduces cognitive overhead when working full-stack.
- **Positive:** Vitest is significantly faster than Karma/Jest for Angular unit tests.
- **Positive:** Playwright provides reliable cross-browser e2e testing with excellent debugging tools.
- **Positive:** Comprehensive ESLint setup catches issues early and enforces modern Angular patterns.
- **Positive:** Nx monorepo with apps/libs gives clear separation between deployable apps and reusable libraries.
- **Negative:** Sheriff adds configuration overhead and a learning curve for import rules.
- **Negative:** Strict boundaries may feel restrictive when two domains need to share data — requires discipline to use `shared/` properly.
- **Negative:** Vitest for Angular is newer than Jest/Karma — some Angular-specific testing utilities may need adaptation.
- **Negative:** Large number of ESLint plugins increases lint time and configuration complexity.
