# Consistent Header Height

> **Status: Implemented** — Merged to main.

## Overview

The page header on pages without an add button (Facturen, Instellingen, Dashboard, Notificaties) renders shorter than pages with an add button (Boekingen, Diensten, Klanten, Medewerkers). The current `min-h-[4rem]` on the shared `<chairly-page-header>` component is not tall enough to match the natural height when a button is present. Increasing the `min-h` value to `min-h-[4.25rem]` ensures all page headers have the same height regardless of whether an action button is projected. This is a frontend-only fix to the existing shared component.

## Domain Context

- Bounded context: Shared UI (cross-cutting)
- Key entities involved: none (UI-only change)
- Ubiquitous language: no new terms; existing page titles remain unchanged (Boekingen, Diensten, Klanten, Medewerkers, Facturen, Instellingen, Dashboard, Notificaties)

## Frontend Tasks

### F1 — Increase min-height on shared page header component

Update the `<chairly-page-header>` component template to use `min-h-[4.25rem]` instead of `min-h-[4rem]`, so the header height matches the natural height when a button is present.

**File:** `libs/shared/src/lib/ui/page-header/page-header.component.html`

**Change:** In the root `<div>`, replace the class `min-h-[4rem]` with `min-h-[4.25rem]`.

**Before:**
```html
<div
  class="flex items-center justify-between border-b border-gray-200 dark:border-slate-700 bg-white dark:bg-slate-800 px-6 py-4 min-h-[4rem]">
```

**After:**
```html
<div
  class="flex items-center justify-between border-b border-gray-200 dark:border-slate-700 bg-white dark:bg-slate-800 px-6 py-4 min-h-[4.25rem]">
```

This is a single-line change. All pages already use `<chairly-page-header>`, so no other files need to be updated -- every page gets the fix automatically.

**Test:** Update the existing unit test to verify the new class value (see F2).

**Playwright e2e scenarios to cover:** No new e2e scenarios required — existing e2e tests cover this component.

### F2 — Update unit test for new min-height class

Update the existing unit test in `page-header.component.spec.ts` that checks for the `min-h` class to assert the new value.

**File:** `libs/shared/src/lib/ui/page-header/page-header.component.spec.ts`

**Change:** In the `should have consistent min-height class` test case, update the assertion from `min-h-[4rem]` to `min-h-[4.25rem]`.

**Before:**
```typescript
it('should have consistent min-height class', () => {
  const rootDiv = fixture.nativeElement.querySelector('div') as HTMLDivElement;
  expect(rootDiv.classList.contains('min-h-[4rem]')).toBe(true);
});
```

**After:**
```typescript
it('should have consistent min-height class', () => {
  const rootDiv = fixture.nativeElement.querySelector('div') as HTMLDivElement;
  expect(rootDiv.classList.contains('min-h-[4.25rem]')).toBe(true);
});
```

No other test cases need to change. All existing tests (title rendering, empty actions container, projected action content) remain valid.

**Playwright e2e scenarios to cover:** No new e2e scenarios required — existing e2e tests cover this component.

## Acceptance Criteria

- [ ] The shared `<chairly-page-header>` component uses `min-h-[4.25rem]` instead of `min-h-[4rem]`
- [ ] Header height is visually identical across all pages, regardless of whether an action button is present
- [ ] The unit test asserts the updated `min-h-[4.25rem]` class
- [ ] All existing unit tests for the page header component still pass
- [ ] All frontend quality checks pass (lint, format:check, test, build)
- [ ] Existing e2e tests still pass

## Out of Scope

- Adding new pages or changing page titles
- Changing button text or click handler names on any page
- Sticky/fixed header behaviour
- Breadcrumbs or sub-navigation within the header
- Backend changes (this is a frontend-only fix)
