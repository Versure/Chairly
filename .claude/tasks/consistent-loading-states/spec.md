# Consistent Loading States

## Overview

Loading indicators are inconsistent across domains. The services domain passes `isLoading` to a child `ServiceTableComponent` which renders a centered "Diensten laden..." `<div>`. The staff and clients domains render a simple left-aligned `<p>Laden...</p>` inline in their list page templates. The `CategoryPanelComponent` renders a minimal centered `<div>` with no spinner. All domains should use a uniform, centered loading indicator with a spinner animation, provided by a shared reusable component in the shared library. Fixes GitHub issue #6.

## Domain Context

- Bounded context: Shared + all domains (Services, Staff, Clients)
- Key entities involved: none (this is a UI-only cross-cutting concern)
- Ubiquitous language: no domain terms affected; all user-facing text in Dutch (Nederlands)
- Key files involved:
  - `libs/shared/src/lib/ui/` -- shared UI component library (contains `ConfirmationDialogComponent`, `ShellComponent`, `ThemeService`)
  - `libs/shared/src/lib/ui/index.ts` -- barrel export for shared UI
  - `libs/chairly/src/lib/services/ui/service-table.component.html` -- current inline loading block (lines 1-4)
  - `libs/chairly/src/lib/services/ui/service-table.component.ts` -- needs `LoadingIndicatorComponent` in imports
  - `libs/chairly/src/lib/services/ui/category-panel.component.html` -- current inline loading block (lines 50-52)
  - `libs/chairly/src/lib/services/ui/category-panel.component.ts` -- needs `LoadingIndicatorComponent` in imports
  - `libs/chairly/src/lib/staff/feature/staff-list-page/staff-list-page.component.html` -- current `<p>Laden...</p>` (line 24)
  - `libs/chairly/src/lib/staff/feature/staff-list-page/staff-list-page.component.ts` -- needs `LoadingIndicatorComponent` in imports
  - `libs/chairly/src/lib/clients/feature/client-list-page/client-list-page.component.html` -- current `<p>Laden...</p>` (line 22)
  - `libs/chairly/src/lib/clients/feature/client-list-page/client-list-page.component.ts` -- needs `LoadingIndicatorComponent` in imports

## Frontend Tasks

### F1 -- Create shared loading indicator component

Create a reusable `<chairly-loading-indicator>` component in the shared UI library.

**Folder:** `libs/shared/src/lib/ui/loading-indicator/`

**Files to create:**
- `loading-indicator.component.ts`
- `loading-indicator.component.html`
- `loading-indicator.component.spec.ts`

**Component details:**
- Selector: `chairly-loading-indicator`
- `ChangeDetectionStrategy.OnPush`, standalone
- No imports array needed (no Angular dependencies used in template besides interpolation)
- `templateUrl: './loading-indicator.component.html'`
- Input: `message` using `input<string>('Laden...')` -- default is Dutch "Laden...", callers can customize (e.g. "Diensten laden...", "Medewerkers laden...", "Klanten laden...")

**Template (`loading-indicator.component.html`):**
```html
<div class="flex items-center justify-center py-12 text-sm text-gray-500 dark:text-slate-400">
  <svg class="mr-2 h-5 w-5 animate-spin text-primary-500" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" aria-hidden="true">
    <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
    <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
  </svg>
  <span>{{ message() }}</span>
</div>
```

**TypeScript (`loading-indicator.component.ts`):**
```typescript
import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'chairly-loading-indicator',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './loading-indicator.component.html',
})
export class LoadingIndicatorComponent {
  readonly message = input<string>('Laden...');
}
```

Follow the pattern of `ConfirmationDialogComponent` -- standalone, OnPush, `templateUrl`, signal-based `input()`.

**Export:** Add `LoadingIndicatorComponent` to `libs/shared/src/lib/ui/index.ts`:
```typescript
export { LoadingIndicatorComponent } from './loading-indicator/loading-indicator.component';
```

**Delete `.gitkeep`:** If a `.gitkeep` file exists in the `loading-indicator/` folder, remove it after adding real files.

**Unit tests (`loading-indicator.component.spec.ts`):**

Test cases:
1. `should create` -- component creates successfully
2. `should display default message "Laden..."` -- render the component without custom input and assert the `<span>` text content is "Laden..."
3. `should display a custom message when provided` -- set input to "Diensten laden..." using `fixture.componentRef.setInput('message', 'Diensten laden...')` and assert the `<span>` text content
4. `should render spinner SVG` -- assert that an `<svg>` element with the `animate-spin` class is present in the DOM
5. `should apply correct dark mode classes` -- assert the container `<div>` has the `dark:text-slate-400` class

Follow the test pattern from `confirmation-dialog.component.spec.ts`:
- Use `TestBed.configureTestingModule({ imports: [LoadingIndicatorComponent] })`
- Use `fixture.componentRef.setInput()` for setting inputs
- Use `fixture.nativeElement.querySelector()` and `textContent` for assertions

### F2 -- Replace loading indicators in all domains

Replace all per-domain loading markup with `<chairly-loading-indicator />`.

**Services table (`service-table.component.html` + `service-table.component.ts`):**

In `service-table.component.html`, replace lines 1-4:
```html
@if (isLoading()) {
  <div class="flex items-center justify-center py-12 text-sm text-gray-500">
    <span>Diensten laden...</span>
  </div>
}
```
with:
```html
@if (isLoading()) {
  <chairly-loading-indicator message="Diensten laden..." />
}
```

In `service-table.component.ts`, add `LoadingIndicatorComponent` to the `imports` array. Import it from `@org/shared-lib`:
```typescript
import { LoadingIndicatorComponent } from '@org/shared-lib';
```
Update imports array: `imports: [CurrencyPipe, DurationPipe, LoadingIndicatorComponent]`

**Category panel (`category-panel.component.html` + `category-panel.component.ts`):**

In `category-panel.component.html`, replace lines 50-52:
```html
  @if (isLoading()) {
    <div class="p-4 text-sm text-gray-500 text-center">Laden...</div>
  }
```
with:
```html
  @if (isLoading()) {
    <chairly-loading-indicator message="Laden..." />
  }
```

In `category-panel.component.ts`, add `LoadingIndicatorComponent` to the `imports` array. Import it from `@org/shared-lib`:
```typescript
import { LoadingIndicatorComponent } from '@org/shared-lib';
```
Update imports array: `imports: [ReactiveFormsModule, LoadingIndicatorComponent]`

**Staff list page (`staff-list-page.component.html` + `staff-list-page.component.ts`):**

In `staff-list-page.component.html`, replace line 24:
```html
      <p class="text-gray-500 dark:text-slate-400">Laden...</p>
```
with:
```html
      <chairly-loading-indicator message="Medewerkers laden..." />
```

In `staff-list-page.component.ts`, add `LoadingIndicatorComponent` to the `imports` array. Import it from `@org/shared-lib` (already has `ConfirmationDialogComponent` from that alias):
```typescript
import { ConfirmationDialogComponent, LoadingIndicatorComponent } from '@org/shared-lib';
```
Update imports array: `imports: [ConfirmationDialogComponent, LoadingIndicatorComponent, StaffFormDialogComponent, StaffTableComponent]`

**Clients list page (`client-list-page.component.html` + `client-list-page.component.ts`):**

In `client-list-page.component.html`, replace line 22:
```html
      <p class="text-gray-500 dark:text-slate-400">Laden...</p>
```
with:
```html
      <chairly-loading-indicator message="Klanten laden..." />
```

In `client-list-page.component.ts`, add `LoadingIndicatorComponent` to the `imports` array. Import it from `@org/shared-lib`:
```typescript
import { ConfirmationDialogComponent, LoadingIndicatorComponent } from '@org/shared-lib';
```
Update imports array: `imports: [ConfirmationDialogComponent, ClientFormDialogComponent, ClientTableComponent, LoadingIndicatorComponent]`

### F3 -- Update unit tests

Update existing unit tests that assert on the old loading markup to work with the new `<chairly-loading-indicator>` component.

**`service-table.component.spec.ts`:**

The test "should show loading indicator when isLoading is true" (lines 114-121) currently asserts:
```typescript
expect(fixture.nativeElement.textContent).toContain('Diensten laden...');
```
This assertion still passes because `LoadingIndicatorComponent` renders the same text. The test should additionally verify that the `<chairly-loading-indicator>` element is present:
```typescript
const loadingEl = fixture.nativeElement.querySelector('chairly-loading-indicator');
expect(loadingEl).toBeTruthy();
expect(fixture.nativeElement.textContent).toContain('Diensten laden...');
```

**`category-panel.component.spec.ts`:**

The test "should show loading indicator when isLoading is true" (lines 38-43) currently asserts:
```typescript
expect(fixture.nativeElement.textContent).toContain('Laden...');
```
Update to also verify the shared component element is present:
```typescript
const loadingEl = fixture.nativeElement.querySelector('chairly-loading-indicator');
expect(loadingEl).toBeTruthy();
expect(fixture.nativeElement.textContent).toContain('Laden...');
```

**`staff-list-page.component.spec.ts`:**

No test currently asserts on the loading state. No changes required unless loading state tests are added. The existing tests already work because they mock `StaffApiService.getAll` to return `of([])` (which resolves immediately, skipping the loading state).

**`client-list-page.component.spec.ts`:**

Same as staff -- no test currently asserts on the loading state. No changes required.

**Important:** Because `ServiceTableComponent` and `CategoryPanelComponent` now import `LoadingIndicatorComponent`, their test modules automatically get the component via the `imports: [ServiceTableComponent]` / `imports: [CategoryPanelComponent]` test setup. No extra test module configuration is needed.

## Acceptance Criteria

- [ ] A shared `<chairly-loading-indicator>` component exists in `libs/shared/src/lib/ui/loading-indicator/`
- [ ] The component is exported from `libs/shared/src/lib/ui/index.ts`
- [ ] The component uses `ChangeDetectionStrategy.OnPush`, standalone, `templateUrl`, signal-based `input()`
- [ ] Default message is "Laden..." (Dutch)
- [ ] Template includes centered layout, spinner SVG with `animate-spin`, and dark mode text class
- [ ] `ServiceTableComponent` uses `<chairly-loading-indicator message="Diensten laden..." />`
- [ ] `CategoryPanelComponent` uses `<chairly-loading-indicator message="Laden..." />`
- [ ] `StaffListPageComponent` uses `<chairly-loading-indicator message="Medewerkers laden..." />`
- [ ] `ClientListPageComponent` uses `<chairly-loading-indicator message="Klanten laden..." />`
- [ ] Loading indicators look identical across all pages (centered, with spinner, Dutch text)
- [ ] Dark mode works correctly on the loading indicator (`dark:text-slate-400` on text, `text-primary-500` on spinner)
- [ ] Unit tests for `LoadingIndicatorComponent` cover: default message, custom message, spinner presence
- [ ] Existing tests for `ServiceTableComponent` and `CategoryPanelComponent` updated to verify `<chairly-loading-indicator>` element
- [ ] All frontend quality checks pass (`npx nx affected -t lint --base=main`, `npx nx format:check --base=main`, `npx nx affected -t test --base=main`, `npx nx affected -t build --base=main`)
- [ ] Existing e2e tests still pass

## Out of Scope

- Skeleton loading / shimmer effects (future enhancement)
- Loading indicators for form submissions (separate concern)
- Backend changes (this is frontend-only)
- Adding loading state tests to `StaffListPageComponent` or `ClientListPageComponent` (they currently have no loading-state assertions and that is a separate concern)
