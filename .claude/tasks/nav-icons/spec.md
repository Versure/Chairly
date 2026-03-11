# Navigation Icons

## Overview

The main sidebar navigation currently only has icons on the "Instellingen" item and the light/dark mode toggle. All other nav items (Boekingen, Diensten, Facturen, Klanten, Medewerkers) lack icons, making the menu look inconsistent and unfinished. This feature adds matching Heroicons (outline style) to every nav item for a polished, consistent sidebar. Fixes GitHub issue #48.

## Domain Context

- Bounded context: Shared (shell/navigation)
- Key entities involved: None (UI-only change)
- Ubiquitous language: N/A (no domain logic)
- Key files:
  - `libs/shared/src/lib/ui/shell/shell.component.html` -- sidebar template
  - `libs/shared/src/lib/ui/shell/shell.component.ts` -- shell component class
  - `libs/shared/src/lib/ui/shell/shell.component.spec.ts` -- unit tests
  - `apps/chairly-e2e/src/navigation.spec.ts` -- e2e navigation tests

## Backend Tasks

_None. This is a frontend-only feature._

## Frontend Tasks

### F1 -- Add SVG icons to all nav items in the sidebar

**File:** `libs/shared/src/lib/ui/shell/shell.component.html`

Add an inline SVG icon (Heroicons outline style, 24x24 viewBox) to each nav item that currently lacks one. The existing "Instellingen" nav item already has a cog/gear icon with `class="h-5 w-5 mr-2"`, `fill="none"`, `stroke="currentColor"`, and `aria-hidden="true"`. All new icons must follow this exact same pattern.

**Icon mapping (Heroicons outline):**

| Nav item     | Heroicons name   | Description                      |
|--------------|------------------|----------------------------------|
| Boekingen    | `calendar-days`  | Calendar with day numbers        |
| Diensten     | `scissors`       | Scissors icon (salon context)    |
| Facturen     | `document-text`  | Document with text lines         |
| Klanten      | `users`          | Two-person silhouette            |
| Medewerkers  | `user-group`     | Group of people                  |

**Each nav item `<a>` should follow this structure** (matching existing Instellingen pattern):

```html
<a
  routerLink="/boekingen"
  routerLinkActive="bg-primary-600"
  (click)="closeSidebar()"
  class="flex items-center px-3 py-2 rounded-md text-sm font-medium text-white hover:bg-primary-600 transition-colors">
  <svg
    xmlns="http://www.w3.org/2000/svg"
    class="h-5 w-5 mr-2"
    fill="none"
    viewBox="0 0 24 24"
    stroke="currentColor"
    aria-hidden="true">
    <!-- Heroicons outline path data here -->
  </svg>
  Boekingen
</a>
```

**Icon requirements:**
- `class="h-5 w-5 mr-2"` -- consistent sizing and spacing (matches existing Instellingen icon)
- `fill="none"` and `stroke="currentColor"` -- outline style, inherits text color
- `stroke-width="2"` on the `<path>` elements (or `stroke-width="1.5"` if using Heroicons v2 24x24)
- `aria-hidden="true"` -- decorative only, the text label provides the accessible name
- `viewBox="0 0 24 24"` -- standard Heroicons viewBox
- `stroke-linecap="round"` and `stroke-linejoin="round"` on paths

**Heroicons SVG path data to use:**

1. **Boekingen** (`calendar-days`):
   ```
   <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5"
     d="M6.75 3v2.25M17.25 3v2.25M3 18.75V7.5a2.25 2.25 0 012.25-2.25h13.5A2.25 2.25 0 0121 7.5v11.25m-18 0A2.25 2.25 0 005.25 21h13.5A2.25 2.25 0 0021 18.75m-18 0v-7.5A2.25 2.25 0 015.25 9h13.5A2.25 2.25 0 0121 11.25v7.5m-9-6h.008v.008H12v-.008zM12 15h.008v.008H12V15zm0 2.25h.008v.008H12v-.008zM9.75 15h.008v.008H9.75V15zm0 2.25h.008v.008H9.75v-.008zM7.5 15h.008v.008H7.5V15zm0 2.25h.008v.008H7.5v-.008zm6.75-4.5h.008v.008h-.008v-.008zm0 2.25h.008v.008h-.008V15zm0 2.25h.008v.008h-.008v-.008zm2.25-4.5h.008v.008H16.5v-.008zm0 2.25h.008v.008H16.5V15z" />
   ```

2. **Diensten** (`scissors`):
   ```
   <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5"
     d="M7.848 8.25l1.536.887M7.848 8.25a3 3 0 11-5.196-3 3 3 0 015.196 3zm1.536.887a2.165 2.165 0 011.083 1.839c.005.351.054.695.14 1.024M9.384 9.137l2.077 1.199M7.848 15.75l1.536-.887m-1.536.887a3 3 0 11-5.196 3 3 3 0 015.196-3zm1.536-.887a2.165 2.165 0 001.083-1.838c.005-.352.054-.695.14-1.025m-1.223 2.863l2.077-1.199m0-3.328a4.323 4.323 0 012.068-1.379l5.325-1.628a4.5 4.5 0 012.48-.044l.803.215-7.794 4.5m-2.882-1.664A4.331 4.331 0 0010.607 12m3.736 0l7.794 4.5-.802.215a4.5 4.5 0 01-2.48-.043l-5.326-1.629a4.324 4.324 0 01-2.068-1.379M14.343 12l-2.882 1.664" />
   ```

3. **Facturen** (`document-text`):
   ```
   <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5"
     d="M19.5 14.25v-2.625a3.375 3.375 0 00-3.375-3.375h-1.5A1.125 1.125 0 0113.5 7.125v-1.5a3.375 3.375 0 00-3.375-3.375H8.25m0 12.75h7.5m-7.5 3H12M10.5 2.25H5.625c-.621 0-1.125.504-1.125 1.125v17.25c0 .621.504 1.125 1.125 1.125h12.75c.621 0 1.125-.504 1.125-1.125V11.25a9 9 0 00-9-9z" />
   ```

4. **Klanten** (`users`):
   ```
   <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5"
     d="M15 19.128a9.38 9.38 0 002.625.372 9.337 9.337 0 004.121-.952 4.125 4.125 0 00-7.533-2.493M15 19.128v-.003c0-1.113-.285-2.16-.786-3.07M15 19.128v.106A12.318 12.318 0 018.624 21c-2.331 0-4.512-.645-6.374-1.766l-.001-.109a6.375 6.375 0 0111.964-3.07M12 6.375a3.375 3.375 0 11-6.75 0 3.375 3.375 0 016.75 0zm8.25 2.25a2.625 2.625 0 11-5.25 0 2.625 2.625 0 015.25 0z" />
   ```

5. **Medewerkers** (`user-group`):
   ```
   <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5"
     d="M18 18.72a9.094 9.094 0 003.741-.479 3 3 0 00-4.682-2.72m.94 3.198l.001.031c0 .225-.012.447-.037.666A11.944 11.944 0 0112 21c-2.17 0-4.207-.576-5.963-1.584A6.062 6.062 0 016 18.719m12 0a5.971 5.971 0 00-.941-3.197m0 0A5.995 5.995 0 0012 12.75a5.995 5.995 0 00-5.058 2.772m0 0a3 3 0 00-4.681 2.72 8.986 8.986 0 003.74.477m.94-3.197a5.971 5.971 0 00-.94 3.197M15 6.75a3 3 0 11-6 0 3 3 0 016 0zm6 3a2.25 2.25 0 11-4.5 0 2.25 2.25 0 014.5 0zm-13.5 0a2.25 2.25 0 11-4.5 0 2.25 2.25 0 014.5 0z" />
   ```

**Important:** The existing Instellingen icon uses `stroke-width="2"` on its paths. The Heroicons v2 (24x24) paths above use `stroke-width="1.5"`. For visual consistency, either update the Instellingen icon to match `stroke-width="1.5"`, or use `stroke-width="2"` on the new icons. Since Heroicons v2 paths are designed for `stroke-width="1.5"`, the recommended approach is to keep the new icons at `stroke-width="1.5"` and update the existing Instellingen icon paths to also use `stroke-width="1.5"` for uniformity.

### F2 -- Normalize the Instellingen icon stroke-width for consistency

**File:** `libs/shared/src/lib/ui/shell/shell.component.html`

The existing Instellingen (cog) icon uses `stroke-width="2"` while Heroicons v2 24x24 paths are designed for `stroke-width="1.5"`. Update the Instellingen icon's `stroke-width` from `"2"` to `"1.5"` on both `<path>` elements so that all nav icons have a uniform stroke weight.

Also update the theme toggle icons (sun and moon) from `stroke-width="2"` to `stroke-width="1.5"` for the same reason.

### F3 -- Add unit test for nav icon rendering

**File:** `libs/shared/src/lib/ui/shell/shell.component.spec.ts`

Add a unit test that verifies all six nav items render an SVG icon. The test should:

1. Query all `<li>` elements inside the sidebar `<ul>`
2. For each `<li>`, verify that the `<a>` element contains an `<svg>` child element
3. Verify each `<svg>` has `aria-hidden="true"`
4. Verify there are exactly 6 nav items with icons (Boekingen, Diensten, Facturen, Klanten, Medewerkers, Instellingen)

```typescript
it('all nav items render an SVG icon', () => {
  const navItems = fixture.nativeElement.querySelectorAll('nav ul li a');
  expect(navItems.length).toBe(6);

  navItems.forEach((link: HTMLElement) => {
    const svg = link.querySelector('svg');
    expect(svg).toBeTruthy();
    expect(svg?.getAttribute('aria-hidden')).toBe('true');
  });
});
```

### F4 -- Add e2e test verifying nav icons are visible

**File:** `apps/chairly-e2e/src/navigation.spec.ts`

Add an e2e test within the existing `'Collapsible sidebar navigation'` test suite that confirms each nav link renders a visible SVG icon. The test should:

1. Navigate to `/`
2. For each nav item name (`Boekingen`, `Diensten`, `Facturen`, `Klanten`, `Medewerkers`, `Instellingen`), locate the link by role and verify it contains a visible `<svg>` child
3. Verify each SVG has `aria-hidden="true"`

```typescript
test('all nav items have an icon', async ({ page }) => {
  await page.goto('/');

  const navItems = ['Boekingen', 'Diensten', 'Facturen', 'Klanten', 'Medewerkers', 'Instellingen'];

  for (const name of navItems) {
    const link = page.getByRole('link', { name });
    await expect(link).toBeVisible();
    const svg = link.locator('svg');
    await expect(svg).toBeVisible();
    await expect(svg).toHaveAttribute('aria-hidden', 'true');
  }
});
```

## Acceptance Criteria

- [ ] All 6 nav items (Boekingen, Diensten, Facturen, Klanten, Medewerkers, Instellingen) have an SVG icon prefix
- [ ] Icons use the Heroicons outline style (stroke, not fill) with a consistent `stroke-width`
- [ ] Icons use `class="h-5 w-5 mr-2"` for consistent sizing and spacing
- [ ] Icons inherit text color via `stroke="currentColor"` so they display correctly in both active (white on bg-primary-600) and inactive (white on bg-primary-700) states
- [ ] Icons are `aria-hidden="true"` (decorative)
- [ ] No layout regressions -- existing spacing, padding, and active state styling unchanged
- [ ] Works correctly in both light and dark mode
- [ ] Unit test verifies all nav items have SVG icons
- [ ] E2e test verifies all nav items have visible SVG icons
- [ ] All frontend quality checks pass (`lint`, `format:check`, `test`, `build`)
- [ ] Existing e2e tests still pass

## Out of Scope

- Adding icons to the mobile hamburger menu toggle button (already has a hamburger icon)
- Icon-only collapsed sidebar mode
- Animated icons or hover effects beyond existing transitions
- Tooltip labels on icons
- Backend changes (none needed)
