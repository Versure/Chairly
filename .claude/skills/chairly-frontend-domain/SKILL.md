---
name: chairly-frontend-domain
description: >
  Chairly frontend domain patterns. Use when implementing Angular domain features:
  API services, NgRx SignalStore, smart components, models, routes, and barrel exports.
  Also use when creating Angular components, pages, stores, services, or any frontend
  code within domain libraries in this project.
user-invocable: false
---

# Chairly Frontend Domain Patterns

Reference boilerplate for implementing a frontend domain feature. All patterns are derived
from existing code in `src/frontend/chairly/libs/chairly/src/lib/services/`. Always read
an existing domain before implementing a new one to confirm nothing has changed.

## Domain Folder Structure

```
libs/chairly/src/lib/{domain}/
├── data-access/
│   ├── {entity}-api.service.ts
│   ├── {entity}.store.ts
│   └── index.ts                   ← barrel: export Store, ApiService, State type
├── feature/
│   └── {entity}-list-page/
│       ├── {entity}-list-page.component.ts
│       └── {entity}-list-page.component.html
├── models/
│   ├── {entity}.models.ts
│   └── index.ts                   ← barrel: export type { ... }
├── ui/
│   ├── {component-name}/
│   │   ├── {component-name}.component.ts
│   │   ├── {component-name}.component.html
│   │   └── {component-name}.component.spec.ts
│   └── index.ts                   ← barrel: export all presentational components
├── pipes/                         ← Angular pipes
├── util/                          ← pure utility functions
└── {domain}.routes.ts             ← route config at domain root
```

## Quick Reference

- API service boilerplate → see `references/service-boilerplate.md` in this skill folder
- NgRx SignalStore boilerplate → see `references/store-boilerplate.md` in this skill folder
- Smart component boilerplate → see `references/component-boilerplate.md` in this skill folder

---

## Models (`models/{entity}.models.ts`)

```typescript
export interface {Entity}Response {
  id: string;
  name: string;
  // domain-specific fields
  createdAtUtc: string;
  createdBy: string;
  updatedAtUtc: string | null;
  updatedBy: string | null;
}

export interface Create{Entity}Request {
  name: string;
  // required fields only (no id, no audit fields)
}

export interface Update{Entity}Request {
  name: string;
  // same shape as Create, minus fields that cannot change
}
```

## Models barrel (`models/index.ts`)

```typescript
export type { {Entity}Response, Create{Entity}Request, Update{Entity}Request } from './{entity}.models';
```

---

## Routes (`{domain}.routes.ts`)

```typescript
import { Route } from '@angular/router';

import { {Entity}ApiService, {Entity}Store } from './data-access';
import { {Entity}ListPageComponent } from './feature';

export const {domain}Routes: Route[] = [
  {
    path: '',
    component: {Entity}ListPageComponent,
    providers: [{Entity}Store, {Entity}ApiService],
  },
];
```

---

## Data-access barrel (`data-access/index.ts`)

```typescript
export type { {Entity}State } from './{entity}.store';
export { {Entity}Store } from './{entity}.store';
export { {Entity}ApiService } from './{entity}-api.service';
```

---

## Sheriff / Module Boundary Rules

- `feature/` → may import from `ui/`, `data-access/`, `models/`, `pipes/`, `util/`
- `ui/` → may import from `models/`, `pipes/`, `util/` only (no data-access)
- `data-access/` → may import from `models/`, `util/` only
- No domain may import from another domain — go through `shared/` only

---

## Checklist Before Creating Files

| File type | Correct folder |
|---|---|
| TypeScript interfaces/DTOs | `models/` |
| Angular `@Pipe` classes | `pipes/` |
| Pure TS utility functions | `util/` |
| Smart (container) components | `feature/{feature-name}/` subfolder |
| Presentational components | `ui/{component-name}/` subfolder |
| Route configuration | `{domain}.routes.ts` at domain root |
| `.gitkeep` | Delete immediately when real files are added |

---

## Component Prefix & UI Language

- Selector prefix: `chairly-` (e.g. `<chairly-booking-list>`)
- All user-facing text must be **Dutch (Nederlands)** — write Dutch from the first keystroke
- Common translations: Save→Opslaan, Cancel→Annuleren, Add→Toevoegen, Edit→Bewerken,
  Delete→Verwijderen, Active→Actief, Inactive→Inactief, Loading→Laden, Confirm→Bevestigen

---

## Entity Selection — No Raw ID Inputs

User-facing forms must **NEVER** ask users to enter IDs (UUIDs/GUIDs). All entity references
must use searchable dropdowns, autocomplete inputs, or selection lists that display the entity
name/label and map the selected entity to its ID internally.

**Wrong:**
```html
<input type="text" formControlName="clientId" placeholder="Voer klant-ID in" />
```

**Correct:**
```html
<select formControlName="clientId">
  @for (client of clients(); track client.id) {
    <option [value]="client.id">{{ client.lastName }}, {{ client.firstName }}</option>
  }
</select>
```

If a related entity needs to be selected (client, staff member, service), the smart component
must load the list of available entities and pass them to the form component. The spec must
define search/filter endpoints if the list is too large for a simple dropdown.

---

## Dark Mode

The dark theme is activated by `data-theme="dark"` on `<html>` (managed by `ThemeService`).

**Rules:**
- Every `tailwind.css` file must include `@custom-variant dark (&:where([data-theme=dark], [data-theme=dark] *));`
  — this prevents OS dark mode from partially activating `dark:` variants without the app's theme control
- Standard Tailwind classes (`bg-white`, `bg-gray-*`, `text-gray-*`) have global dark overrides in `tailwind.css`
- Custom/brand colors (`bg-primary-*`, `bg-accent-*`) have NO global override — always add an explicit
  `dark:` variant in the template (e.g. `bg-primary-50 dark:bg-slate-800`)
- When adding any light-mode background, always pair with a `dark:` variant
- Text contrast: use `text-gray-700` or darker for body text on light backgrounds (never `text-gray-500`
  or `text-gray-600` for main content — insufficient contrast)

---

## Page Layout — Sticky Footer

Pages with header + footer must use flex column layout to prevent the footer from floating
when content is shorter than the viewport:

```scss
:host {
  display: flex;
  flex-direction: column;
  min-height: 100vh;
}
```

```html
<app-header />
<main class="flex-1"><!-- content --></main>
<app-footer />
```

---

## Forbidden

- No `any` types
- No `console` statements
- No `@Input()`/`@Output()`/`@ViewChild()` — use `input()`, `model()`, `viewChild()`, `OutputEmitterRef`
- No `Subject` + `ngOnDestroy` — use `takeUntilDestroyed(destroyRef)`
- No function calls in templates — use signals or pipes
- No inline `template:` — always `templateUrl:` with a separate `.html` file
- No `imports: []` when component has no imports — omit the property entirely
- No model interfaces or pipes inside `util/`
- No English UI text
