# Consistent Page Header

## Overview

The page header (title + optional action button) is inconsistent across domains. Pages with an "add" button (e.g. Boekingen, Diensten, Klanten, Medewerkers) have a taller header because of the button. Pages without an "add" button (Facturen list, Instellingen) render a shorter header because there is nothing next to the title, causing a visible height difference. All domain page headers should have the same height regardless of whether an action button is present. Fixes GitHub issues #41 and related visual inconsistencies.

---

## Domain Context

- **Bounded context:** Shared + all domains (Billing, Settings)
- **Key files involved:**
  - `libs/shared/src/lib/ui/` — new shared page header component
  - All domain list pages and the settings page that need consistent headers

---

## Frontend Tasks

### F1 — Create shared page header component

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

The component exposes a named content slot (`ng-content select="[actions]"`) for optional action buttons. When no `[actions]` content is projected, the right side of the header is left empty but still reserves the height of a button via `min-height` or a fixed height on the flex container.

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

Export from `libs/shared/src/lib/ui/index.ts`.

### F2 — Replace inline headers with the shared component

Replace the inline header markup in all domain list pages and the settings page with `<chairly-page-header>`.

**Invoices list page** (`libs/chairly/src/lib/billing/feature/invoice-list/invoice-list-page.component.html`):

```html
<chairly-page-header title="Facturen" />
```

No action button — the right side is empty but the header height matches other pages.

**Settings page** (`libs/chairly/src/lib/settings/feature/...`):

```html
<chairly-page-header title="Instellingen" />
```

**Bookings list page:**

```html
<chairly-page-header title="Boekingen">
  <button actions class="btn-primary" (click)="onAddBooking()">Nieuwe boeking</button>
</chairly-page-header>
```

**Services list page:**

```html
<chairly-page-header title="Diensten">
  <button actions class="btn-primary" (click)="onAddService()">Nieuwe dienst</button>
</chairly-page-header>
```

**Clients list page:**

```html
<chairly-page-header title="Klanten">
  <button actions class="btn-primary" (click)="onAddClient()">Nieuwe klant</button>
</chairly-page-header>
```

**Staff list page:**

```html
<chairly-page-header title="Medewerkers">
  <button actions class="btn-primary" (click)="onAddStaffMember()">Nieuwe medewerker</button>
</chairly-page-header>
```

Add `PageHeaderComponent` to each component's imports. Remove the old inline header markup.

### F3 — Unit test for page header component

Write unit tests for `PageHeaderComponent`:
- Renders the title correctly
- Right side is empty when no `[actions]` content is projected
- Renders projected action content when provided
- Component height is consistent (check that `min-h-[4rem]` class is present)

---

## Acceptance Criteria

- [ ] A shared `<chairly-page-header>` component exists in `libs/shared/src/lib/ui/page-header/`
- [ ] Component accepts a required `title` input and an optional `[actions]` content slot
- [ ] Header height is identical across all domain list pages and the settings page, regardless of whether an action button is present
- [ ] Invoices list page header is the same height as Boekingen, Diensten, Klanten, and Medewerkers list pages
- [ ] Settings page header is the same height as other domain pages
- [ ] Dark mode styling is correct on the shared component
- [ ] All frontend quality checks pass (lint, test, build)
- [ ] Existing e2e tests still pass

---

## Out of Scope

- Breadcrumbs or sub-navigation within the header
- Secondary action buttons or dropdown menus in the header
- Sticky/fixed header behaviour
