# Consistent Loading States

## Overview

Loading indicators are inconsistent across domains. The services domain passes `isLoading` to a child table component which renders a centered "Diensten laden..." message. The staff and clients domains render a simple left-aligned `<p>Laden...</p>` in the list page template. All domains should use a uniform, centered loading indicator pattern. Fixes GitHub issue #6.

## Domain Context

- Bounded context: Shared + all domains (Services, Staff, Clients)
- Key files involved:
  - `libs/shared/src/lib/ui/` — new shared loading component
  - `libs/chairly/src/lib/services/ui/service-table.component.html` — currently has its own loading
  - `libs/chairly/src/lib/services/feature/service-list-page/service-list-page.component.html`
  - `libs/chairly/src/lib/staff/feature/staff-list-page/staff-list-page.component.html`
  - `libs/chairly/src/lib/clients/feature/client-list-page/client-list-page.component.html`

## Frontend Tasks

### F1 — Create shared loading indicator component

Create a reusable `<chairly-loading-indicator>` component in the shared library.

**Folder:** `libs/shared/src/lib/ui/loading-indicator/`

**Files:**
- `loading-indicator.component.ts`
- `loading-indicator.component.html`

**Component details:**
- Selector: `chairly-loading-indicator`
- `ChangeDetectionStrategy.OnPush`, standalone
- Input: `message` (`input<string>()`, default `'Laden...'`) — allows per-domain customization like "Diensten laden..."
- Template: centered text with a subtle spinner/animation
  ```html
  <div class="flex items-center justify-center py-12 text-sm text-gray-500 dark:text-slate-400">
    <svg class="mr-2 h-5 w-5 animate-spin text-primary-500" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" aria-hidden="true">
      <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
      <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
    </svg>
    <span>{{ message() }}</span>
  </div>
  ```

Export from `libs/shared/src/lib/ui/index.ts`.

### F2 — Replace loading indicators in all domains

Replace all per-domain loading implementations with the shared component.

**Staff list page** (`staff-list-page.component.html`):
- Replace `<p class="text-gray-500 dark:text-slate-400">Laden...</p>` with:
  ```html
  <chairly-loading-indicator message="Medewerkers laden..." />
  ```
- Add `LoadingIndicatorComponent` to the component's imports

**Clients list page** (`client-list-page.component.html`):
- Replace `<p class="text-gray-500 dark:text-slate-400">Laden...</p>` with:
  ```html
  <chairly-loading-indicator message="Klanten laden..." />
  ```
- Add `LoadingIndicatorComponent` to the component's imports

**Services table** (`service-table.component.html`):
- Replace the inline loading `<div>` block with:
  ```html
  <chairly-loading-indicator message="Diensten laden..." />
  ```
- Add `LoadingIndicatorComponent` to the component's imports

**Category panel** (`category-panel.component.html`):
- Replace the inline loading `<div>` with:
  ```html
  <chairly-loading-indicator message="Laden..." />
  ```
- Add `LoadingIndicatorComponent` to the component's imports

### F3 — Update unit tests

Update or add unit tests for the `LoadingIndicatorComponent`:
- Default message is "Laden..."
- Custom message renders correctly
- Spinner SVG is present

Update existing component tests if they assert on the old loading markup.

## Acceptance Criteria

- [ ] A shared `<chairly-loading-indicator>` component exists in `libs/shared/src/lib/ui/loading-indicator/`
- [ ] All domains (services, staff, clients) use the shared loading component
- [ ] Loading indicators look identical across all pages (centered, with spinner, Dutch text)
- [ ] Each domain can pass a custom message (e.g. "Diensten laden...", "Medewerkers laden...")
- [ ] Dark mode works correctly on the loading indicator
- [ ] All frontend quality checks pass (lint, test, build)
- [ ] Existing e2e tests still pass

## Out of Scope

- Skeleton loading / shimmer effects (future enhancement)
- Loading indicators for form submissions (separate concern)
