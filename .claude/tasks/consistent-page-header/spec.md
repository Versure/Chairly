# Consistent Page Header

## Overview

The page header (title + optional action button) is inconsistent across domains. Pages with an "add" button (Boekingen, Diensten, Klanten, Medewerkers) have a taller header because the button adds vertical height. Pages without an "add" button (Facturen list, Instellingen) render a shorter header because there is nothing beside the title. All domain page headers should have the same height regardless of whether an action button is present. This is a frontend-only feature that creates a shared component and replaces inline header markup across all domain pages. Fixes GitHub issues #41 and related visual inconsistencies.

## Domain Context

- Bounded context: Shared UI + all domain pages (Bookings, Services, Clients, Staff, Billing, Settings)
- Key entities involved: none (UI-only change)
- Ubiquitous language: no new terms; existing page titles remain unchanged (Boekingen, Diensten, Klanten, Medewerkers, Facturen, Instellingen)

## Frontend Tasks

### F1 -- Create shared page header component

Extract the page header into a reusable `<chairly-page-header>` component so all pages use the same markup and height.

**Folder:** `libs/shared/src/lib/ui/page-header/`

**Files:**
- `page-header.component.ts`
- `page-header.component.html`

**Component API:**

```typescript
// Selector: chairly-page-header
// ChangeDetectionStrategy.OnPush, standalone

title = input.required<string>();
```

The component exposes a named content slot (`ng-content select="[actions]"`) for optional action buttons. When no `[actions]` content is projected, the right side of the header is left empty but still reserves the height of a button via `min-h-[4rem]` on the flex container.

**Template:**

```html
<div class="flex items-center justify-between border-b border-gray-200 dark:border-slate-700
            bg-white dark:bg-slate-800 px-6 py-4 min-h-[4rem]">
  <h1 class="text-xl font-semibold text-gray-900 dark:text-slate-100">{{ title() }}</h1>
  <div class="flex items-center gap-2">
    <ng-content select="[actions]" />
  </div>
</div>
```

The `min-h-[4rem]` ensures the header is always at least 64px tall, matching the height of a row with a standard button (`py-2` + font + border).

**Component TypeScript structure:**

- `standalone: true`
- `changeDetection: ChangeDetectionStrategy.OnPush`
- `selector: 'chairly-page-header'`
- `templateUrl: './page-header.component.html'`
- No `imports` property (no dependencies needed)
- Use `input.required<string>()` for the title (signal-based API, not `@Input()`)

**Export:** Add `PageHeaderComponent` to `libs/shared/src/lib/ui/index.ts`.

### F2 -- Replace inline headers with the shared component

Replace the inline header markup in all domain list pages and the settings page with `<chairly-page-header>`. The existing button text and click handler names must be preserved exactly as they are in the current codebase.

**Pages to update (6 total):**

1. **Invoices list page** (`libs/chairly/src/lib/billing/feature/invoice-list-page/invoice-list-page.component.html`)

   Current markup (lines 3-6):
   ```html
   <div class="flex items-center justify-between border-b border-gray-200 bg-white px-6 py-4 dark:border-slate-700 dark:bg-slate-800">
     <h1 class="text-xl font-semibold text-gray-900 dark:text-white">Facturen</h1>
   </div>
   ```

   Replace with:
   ```html
   <chairly-page-header title="Facturen" />
   ```

   No action button -- the right side is empty but the header height matches other pages.

   **Component TS changes:** Add `PageHeaderComponent` to `imports` array (import from `@org/shared-lib`).

2. **Settings page** (`libs/chairly/src/lib/settings/feature/settings-page/settings-page.component.html`)

   Current markup (lines 3-6):
   ```html
   <div class="flex items-center justify-between border-b border-gray-200 bg-white px-6 py-4 dark:border-slate-700 dark:bg-slate-800">
     <h1 class="text-xl font-semibold text-gray-900 dark:text-white">Instellingen</h1>
   </div>
   ```

   Replace with:
   ```html
   <chairly-page-header title="Instellingen" />
   ```

   No action button.

   **Component TS changes:** Add `PageHeaderComponent` to `imports` array (import from `@org/shared-lib`).

3. **Bookings list page** (`libs/chairly/src/lib/bookings/feature/booking-list-page/booking-list-page.component.html`)

   Current markup (lines 3-11):
   ```html
   <div class="flex items-center justify-between border-b border-gray-200 bg-white px-6 py-4">
     <h1 class="text-xl font-semibold text-gray-900">Boekingen</h1>
     <button type="button" class="..." (click)="onAddBooking()">
       Nieuwe boeking
     </button>
   </div>
   ```

   Replace with:
   ```html
   <chairly-page-header title="Boekingen">
     <button actions type="button"
       class="inline-flex items-center rounded-md bg-primary-600 px-4 py-2 text-sm font-medium text-white hover:bg-primary-700 focus:outline-none focus:ring-2 focus:ring-primary-500 focus:ring-offset-2 dark:bg-primary-500 dark:hover:bg-primary-600"
       (click)="onAddBooking()">
       Nieuwe boeking
     </button>
   </chairly-page-header>
   ```

   **Component TS changes:** Add `PageHeaderComponent` to `imports` array (import from `@org/shared-lib`).

4. **Services list page** (`libs/chairly/src/lib/services/feature/service-list-page/service-list-page.component.html`)

   Current markup (lines 3-12):
   ```html
   <div class="flex items-center justify-between border-b border-gray-200 bg-white px-6 py-4">
     <h1 class="text-xl font-semibold text-gray-900">Diensten</h1>
     <button type="button" class="..." (click)="openAddService()">
       Dienst toevoegen
     </button>
   </div>
   ```

   Replace with:
   ```html
   <chairly-page-header title="Diensten">
     <button actions type="button"
       class="inline-flex items-center rounded-md bg-primary-600 px-4 py-2 text-sm font-medium text-white hover:bg-primary-700 focus:outline-none focus:ring-2 focus:ring-primary-500 focus:ring-offset-2 dark:bg-primary-500 dark:hover:bg-primary-600"
       (click)="openAddService()">
       Dienst toevoegen
     </button>
   </chairly-page-header>
   ```

   **Component TS changes:** Add `PageHeaderComponent` to `imports` array (import from `@org/shared-lib`).

5. **Clients list page** (`libs/chairly/src/lib/clients/feature/client-list-page/client-list-page.component.html`)

   Current markup (lines 3-12):
   ```html
   <div class="flex items-center justify-between border-b border-gray-200 bg-white px-6 py-4 dark:border-slate-700 dark:bg-slate-800">
     <h1 class="text-xl font-semibold text-gray-900 dark:text-white">Klanten</h1>
     <button type="button" class="..." (click)="openAddDialog()">
       + Klant toevoegen
     </button>
   </div>
   ```

   Replace with:
   ```html
   <chairly-page-header title="Klanten">
     <button actions type="button"
       class="inline-flex items-center rounded-md bg-primary-600 px-4 py-2 text-sm font-medium text-white hover:bg-primary-700 focus:outline-none focus:ring-2 focus:ring-primary-500 focus:ring-offset-2 dark:bg-primary-500 dark:hover:bg-primary-600"
       (click)="openAddDialog()">
       + Klant toevoegen
     </button>
   </chairly-page-header>
   ```

   **Component TS changes:** Add `PageHeaderComponent` to `imports` array (import from `@org/shared-lib`).

6. **Staff list page** (`libs/chairly/src/lib/staff/feature/staff-list-page/staff-list-page.component.html`)

   Current markup (lines 3-12):
   ```html
   <div class="flex items-center justify-between border-b border-gray-200 bg-white px-6 py-4 dark:border-slate-700 dark:bg-slate-800">
     <h1 class="text-xl font-semibold text-gray-900 dark:text-white">Medewerkers</h1>
     <button type="button" class="..." (click)="openAddDialog()">
       + Medewerker toevoegen
     </button>
   </div>
   ```

   Replace with:
   ```html
   <chairly-page-header title="Medewerkers">
     <button actions type="button"
       class="inline-flex items-center rounded-md bg-primary-600 px-4 py-2 text-sm font-medium text-white hover:bg-primary-700 focus:outline-none focus:ring-2 focus:ring-primary-500 focus:ring-offset-2 dark:bg-primary-500 dark:hover:bg-primary-600"
       (click)="openAddDialog()">
       + Medewerker toevoegen
     </button>
   </chairly-page-header>
   ```

   **Component TS changes:** Add `PageHeaderComponent` to `imports` array (import from `@org/shared-lib`).

**Important notes for all pages:**
- Keep the `<!-- Page header -->` comment above the new component for readability.
- Remove the old `<div>` wrapper and its contents; the `<chairly-page-header>` replaces it entirely.
- The `actions` attribute on the button is what the `ng-content select="[actions]"` targets -- it must be present.
- Preserve the existing button text and click handler names exactly as they are in each page.
- Import `PageHeaderComponent` from `@org/shared-lib` in each component's TypeScript file.

### F3 -- Unit test for page header component

Write unit tests for `PageHeaderComponent` in `libs/shared/src/lib/ui/page-header/page-header.component.spec.ts`.

**Test cases:**

1. **Renders the title correctly** -- Create the component with a title input and verify the `<h1>` element displays the expected text.

2. **Right side is empty when no `[actions]` content is projected** -- Create the component without projected content and verify the actions container (`div.flex.items-center.gap-2`) is empty.

3. **Renders projected action content when provided** -- Create a host component that projects a `<button actions>` into the page header and verify the button is rendered inside the actions container.

4. **Has consistent min-height class** -- Verify the root container `<div>` has the `min-h-[4rem]` class, ensuring consistent height regardless of content.

**Test setup:**
- Use Vitest (the project standard, not Jest or Karma)
- Use `TestBed.configureTestingModule` with the standalone component
- For projection tests, create a minimal host component with `@Component({ template: '...' })`
- Follow the existing test patterns seen in `libs/shared/src/lib/ui/loading-indicator/loading-indicator.component.spec.ts` and `libs/shared/src/lib/ui/confirmation-dialog/confirmation-dialog.component.spec.ts`

## Acceptance Criteria

- [ ] A shared `<chairly-page-header>` component exists in `libs/shared/src/lib/ui/page-header/`
- [ ] Component accepts a required `title` input and an optional `[actions]` content slot
- [ ] Component is exported from `libs/shared/src/lib/ui/index.ts`
- [ ] Header height is identical across all domain list pages and the settings page, regardless of whether an action button is present (enforced by `min-h-[4rem]`)
- [ ] Invoices list page uses `<chairly-page-header title="Facturen" />` with no action button
- [ ] Settings page uses `<chairly-page-header title="Instellingen" />` with no action button
- [ ] Bookings list page uses `<chairly-page-header>` with the existing "Nieuwe boeking" button projected via `[actions]`
- [ ] Services list page uses `<chairly-page-header>` with the existing "Dienst toevoegen" button projected via `[actions]`
- [ ] Clients list page uses `<chairly-page-header>` with the existing button projected via `[actions]`
- [ ] Staff list page uses `<chairly-page-header>` with the existing button projected via `[actions]`
- [ ] Dark mode styling is correct on the shared component (`dark:border-slate-700`, `dark:bg-slate-800`, `dark:text-slate-100`)
- [ ] Unit tests pass for the page header component
- [ ] All frontend quality checks pass (`lint`, `format:check`, `test`, `build`)
- [ ] Existing e2e tests still pass

## Out of Scope

- Breadcrumbs or sub-navigation within the header
- Secondary action buttons or dropdown menus in the header
- Sticky/fixed header behaviour
- Changing existing button text or handler names (keep current labels)
- Backend changes (this is a frontend-only feature)
