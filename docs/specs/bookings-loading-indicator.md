# Bookings Loading Indicator

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

**File:** `libs/chairly/src/lib/bookings/feature/booking-list-page/booking-list-page.component.html`

In the `<!-- Content -->` section, replace:
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

**File:** `libs/chairly/src/lib/bookings/feature/booking-list-page/booking-list-page.component.ts`

Add `LoadingIndicatorComponent` to the component's `imports` array. Import from `@org/shared-lib`:
```typescript
import { LoadingIndicatorComponent } from '@org/shared-lib';
```
Update `imports` array: `imports: [BookingFormDialogComponent, BookingScheduleComponent, BookingTableComponent, LoadingIndicatorComponent]`

**Unit test** (add to `booking-list-page.component.spec.ts` if it exists, or skip if no spec file):
- If a spec file exists, add a test: `should show loading indicator when isLoading is true` — set `isLoading` to true, verify `<chairly-loading-indicator>` element is present in the DOM.

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
