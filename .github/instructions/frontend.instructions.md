---
applyTo: "src/frontend/**"
---

# Frontend Conventions — Nx Monorepo with DDD & Sheriff

## Workspace

`src/frontend/chairly/` — Nx monorepo with apps/libs layout.

## Structure

```
src/frontend/chairly/
├── apps/
│   ├── chairly/              # Main Angular application
│   └── chairly-e2e/          # Playwright e2e tests
├── libs/
│   ├── chairly/src/lib/      # Domain library
│   │   ├── bookings/         # Each domain has the layers below
│   │   ├── clients/          #   feature/{feature-name}/ — smart components
│   │   ├── staff/            #   ui/ — presentational components
│   │   ├── services/         #   data-access/ — stores, API services
│   │   ├── billing/          #   models/ — TypeScript interfaces/types
│   │   └── notifications/    #   pipes/, util/, {domain}.routes.ts
│   └── shared/src/lib/       # Shared library (ui/, data-access/, util/)
```

## Path Aliases

`@org/chairly-lib`, `@org/shared-lib`

## Module Boundary Rules (Sheriff)

- A domain cannot import from another domain
- A domain can import from `shared/`
- `shared/` cannot import from any domain
- Within a domain: `feature/` -> `ui/`, `data-access/`, `models/`, `pipes/`, `util/`
- `ui/` -> `models/`, `pipes/`, `util/` only
- `data-access/` -> `models/`, `util/` only

## File Placement

| File type | Correct folder |
|---|---|
| TypeScript interfaces/DTOs | `models/` |
| Angular `@Pipe` classes | `pipes/` |
| Pure TS utility functions | `util/` |
| Smart (container) components | `feature/{feature-name}/` subfolder |
| Presentational components | `ui/{component-name}/` subfolder |
| Route configuration | `{domain}.routes.ts` at domain root |

## Code Conventions

- Standalone components, no NgModules
- OnPush change detection on all components
- Signal-based APIs: `input()`, `model()`, `viewChild()`, `OutputEmitterRef`
- NgRx SignalStore for shared/feature state
- `takeUntilDestroyed(destroyRef)` for subscription cleanup (always inject `DestroyRef` explicitly)
- Smart/Dumb component pattern
- Tailwind CSS v4 for styling, SCSS for component styles
- Reactive forms with typed FormGroups
- Lazy-loaded routes per domain
- Component prefix: `chairly-`
- Always use `templateUrl:` with a separate `.html` file (no inline `template:`)
- Omit `imports: []` when component has no imports
- Self-closing tags for empty elements
- Explicit return types on all functions
- No `any` types, no `console` statements

## UI Language — Dutch (Nederlands)

All user-facing text must be Dutch from the first keystroke.
Common translations: Save->Opslaan, Cancel->Annuleren, Add->Toevoegen, Edit->Bewerken,
Delete->Verwijderen, Active->Actief, Inactive->Inactief, Loading->Laden, Confirm->Bevestigen

## Locale Configuration

In `app.config.ts`: `registerLocaleData(localeNl)`, provide `LOCALE_ID: 'nl-NL'` and `DEFAULT_CURRENCY_CODE: 'EUR'`.

## Dark Mode

`data-theme="dark"` on `<html>` (managed by `ThemeService`).
Always pair light-mode background colors with a `dark:` Tailwind variant.
Custom/brand colors (`bg-primary-*`, `bg-accent-*`) have no global dark override — always add explicit `dark:` variant.

## Native `<dialog>` Pattern

Use `showModal()` with full-screen overlay. Inject `DOCUMENT`, toggle `body.style.overflow`.
Close via `page.keyboard.press('Escape')` in e2e tests.

## Styles Setup

- `apps/chairly/src/tailwind.css` — `@import 'tailwindcss'` + `@source` directives (plain CSS, never SCSS)
- `apps/chairly/src/styles.scss` — SCSS globals (variables, mixins, fonts). Never merge with tailwind.css.
- `postcss.config.json` is used by Angular builder (not `.mjs`)

## Quality Checks

```bash
cd src/frontend/chairly
npx nx affected -t lint --base=main
npx nx format:check --base=main
npx nx affected -t test --base=main
npx nx affected -t build --base=main
```

If `npx nx format:check` fails, auto-fix with `npx nx format --base=main` then verify again.
