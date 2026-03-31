# Bookings Loading Indicator

> **Status: Implemented** — Merged to main.

## Overview

The bookings domain renders a plain `<p class="text-gray-500 dark:text-gray-400">Laden...</p>` while data is loading. All other domains (services, staff, clients) already use the shared `<chairly-loading-indicator>` component from `@org/shared-lib`. This spec brings bookings in line with the rest. Fixes GitHub issue #38.

## Domain Context

- Bounded context: Bookings + Shared
- Key entities involved: none (UI-only cross-cutting concern)
- Ubiquitous language: no domain terms affected; all user-facing text in Dutch
- Key files:
  - `libs/chairly/src/lib/bookings/feature/booking-list-page/booking-list-page.component.html` — current plain `<p>Laden...</p>` at line 111
  - `libs/chairly/src/lib/bookings/feature/booking-list-page/booking-list-page.component.ts` — needs `LoadingIndicatorComponent` in imports
  - `libs/shared/src/lib/ui/loading-indicator/loading-indicator.component.ts` — already exists, already exported from `@org/shared-lib`

## Frontend Tasks

### F1 — Replace loading indicator in bookings list page

Replace the plain loading paragraph in `booking-list-page.component.html` with the shared `<chairly-loading-indicator>` component.

**Template change (`booking-list-page.component.html`):**

In the `<!-- Content -->` section (around line 110-111), replace:
```html
@if (isLoading()) {
  <p class="text-gray-500 dark:text-gray-400">Laden...</p>
}
```
with:
```html
@if (isLoading()) {
  <chairly-loading-indicator message="Boekingen laden..." />
}
```

**Component change (`booking-list-page.component.ts`):**

1. Add the import statement for `LoadingIndicatorComponent` from `@org/shared-lib`. It should be grouped with the existing `@org/shared-lib` import (`InvoiceGenerationService`):
   ```typescript
   import { InvoiceGenerationService, LoadingIndicatorComponent } from '@org/shared-lib';
   ```
   (Merge into the existing import line — do not add a second import from `@org/shared-lib`.)

2. Add `LoadingIndicatorComponent` to the `imports` array of the `@Component` decorator:
   ```typescript
   imports: [BookingFormDialogComponent, BookingScheduleComponent, BookingTableComponent, LoadingIndicatorComponent],
   ```
   (Keep alphabetical order within the array.)

**Unit test (`booking-list-page.component.spec.ts`):**

No spec file currently exists for this component (`booking-list-page.component.spec.ts` does not exist). Skip unit test creation for this task — the existing e2e tests and the shared `LoadingIndicatorComponent` unit tests already cover the loading indicator behavior.

**Verification:**

After making the changes, verify consistency with other domains by comparing to:
- `staff-list-page.component.html` — uses `<chairly-loading-indicator message="Medewerkers laden..." />`
- `client-list-page.component.html` — uses `<chairly-loading-indicator message="Klanten laden..." />`

The pattern is identical: `@if (isLoading()) { <chairly-loading-indicator message="{Domain} laden..." /> }`.

## Acceptance Criteria

- [ ] `<chairly-loading-indicator message="Boekingen laden..." />` replaces the plain paragraph in `booking-list-page.component.html`
- [ ] `LoadingIndicatorComponent` is imported from `@org/shared-lib` and listed in the `imports` array of `BookingListPageComponent`
- [ ] Loading indicator is centered, has spinner, and displays "Boekingen laden..." — consistent with other domains
- [ ] Dark mode works correctly (no light block in dark mode)
- [ ] All frontend quality checks pass (`npx nx affected -t lint --base=main`, `npx nx format:check --base=main`, `npx nx affected -t test --base=main`, `npx nx affected -t build --base=main`)
- [ ] Existing e2e tests still pass

## Out of Scope

- Backend changes (frontend-only fix)
- Adding loading indicators to the schedule view or form dialog
- Skeleton/shimmer loading effects
- Creating a unit test file for `BookingListPageComponent` (no spec file exists today)
