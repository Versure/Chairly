# Website Footer Sticky

> **Status: Implemented** — Merged to main.

## Overview

The footer on the Chairly public website (`chairly-website` app) does not stick to the bottom of the viewport on pages with little content, such as the confirmation page (`/bevestiging`). Currently, each page component individually includes `<chairly-web-header />` and `<chairly-web-footer />` in its own template, and there is no shared layout enforcing a full-height flex column. This feature introduces a dedicated `WebsiteLayoutComponent` that wraps all routed pages with a sticky-footer layout (flex column, min-height 100vh), and removes the duplicated header/footer from individual page components.

## Domain Context

- Bounded context: **Onboarding** (public website)
- Key entities involved: none (this is a layout/UI-only concern)
- Ubiquitous language: no ubiquitous language terms from `docs/domain-model.md` are affected by this change; this is a layout-only concern with no domain entity involvement
- Key files involved:
  - `apps/chairly-website/src/app/app.routes.ts` -- app route config
  - `libs/website/src/lib/onboarding/onboarding.routes.ts` -- onboarding route definitions
  - `libs/website/src/lib/onboarding/ui/header/` -- existing header component
  - `libs/website/src/lib/onboarding/ui/footer/` -- existing footer component
  - `libs/website/src/lib/onboarding/feature/landing-page/` -- landing page (includes header + footer)
  - `libs/website/src/lib/onboarding/feature/pricing-page/` -- pricing page (includes header + footer)
  - `libs/website/src/lib/onboarding/feature/subscribe-page/` -- subscribe page (includes header + footer)
  - `libs/website/src/lib/onboarding/feature/confirmation-page/` -- confirmation page (includes header + footer)
  - `apps/chairly-website-e2e/src/` -- Playwright e2e tests

## Backend Tasks

No backend tasks — this is a frontend-only change.

## Frontend Tasks

### F1 — Create WebsiteLayoutComponent

Create a `WebsiteLayoutComponent` in the website lib that serves as a parent route wrapper providing the sticky-footer layout pattern.

**Folder:** `libs/website/src/lib/onboarding/ui/website-layout/`

**Files to create:**
- `website-layout.component.ts`
- `website-layout.component.html`
- `website-layout.component.scss`
- `website-layout.component.spec.ts`

**Component details:**
- Selector: `chairly-web-layout`
- `ChangeDetectionStrategy.OnPush`, standalone
- Imports: `RouterOutlet`, `HeaderComponent`, `FooterComponent`
- `templateUrl: './website-layout.component.html'`

**Template (`website-layout.component.html`):**
```html
<div class="flex min-h-screen flex-col">
  <chairly-web-header />
  <main class="flex-1">
    <router-outlet />
  </main>
  <chairly-web-footer />
</div>
```

The `min-h-screen` class ensures the container stretches to at least the full viewport height. The `flex-1` on `<main>` makes the content area grow to fill remaining space, pushing the footer to the bottom even when content is short.

**SCSS (`website-layout.component.scss`):** Empty file (all styling via Tailwind classes in the template).

**Export:** Add `WebsiteLayoutComponent` to `libs/website/src/lib/onboarding/ui/index.ts`:
```typescript
export { WebsiteLayoutComponent } from './website-layout/website-layout.component';
```

**Unit tests (`website-layout.component.spec.ts`):**

Test cases:
1. `should create` -- component creates successfully
2. `should render header component` -- assert `chairly-web-header` element is present
3. `should render footer component` -- assert `chairly-web-footer` element is present
4. `should render router-outlet` -- assert `router-outlet` element is present
5. `should have min-h-screen flex container` -- assert the root `<div>` has classes `flex`, `min-h-screen`, `flex-col`
6. `should have flex-1 on main element` -- assert the `<main>` element has the `flex-1` class

Use `TestBed.configureTestingModule` with `imports: [WebsiteLayoutComponent]` and provide `RouterModule.forRoot([])` (or `provideRouter([])`) for the router outlet.

### F2 — Wire layout component into routes and remove duplicate header/footer

Update the route configuration to use `WebsiteLayoutComponent` as a parent wrapper for all onboarding pages, and remove the duplicated `<chairly-web-header />` and `<chairly-web-footer />` from each individual page component.

**Route changes:**

In `libs/website/src/lib/onboarding/onboarding.routes.ts`, wrap all child routes under a parent route that uses `WebsiteLayoutComponent`:

```typescript
import { Routes } from '@angular/router';

export const onboardingRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./ui').then((m) => m.WebsiteLayoutComponent),
    children: [
      {
        path: '',
        loadComponent: () => import('./feature').then((m) => m.LandingPageComponent),
      },
      {
        path: 'prijzen',
        loadComponent: () => import('./feature').then((m) => m.PricingPageComponent),
      },
      {
        path: 'abonneren',
        loadComponent: () => import('./feature').then((m) => m.SubscribePageComponent),
      },
      {
        path: 'bevestiging',
        loadComponent: () => import('./feature').then((m) => m.ConfirmationPageComponent),
      },
    ],
  },
];
```

**Remove header and footer from page components:**

For each of the four page components, remove `<chairly-web-header />` and `<chairly-web-footer />` from the template, and remove the `HeaderComponent` and `FooterComponent` from the component's `imports` array.

**Landing page (`landing-page.component.html`):**
- Remove line 1: `<chairly-web-header />`
- Remove last line: `<chairly-web-footer />`
- Keep the `<main class="flex-1">` wrapper and all its content. Note: the `<main>` tag in the landing page template should be changed to a `<div>` (or removed entirely, keeping just its children) since the layout component already provides the `<main>` wrapper. Alternatively, keep it as a `<div class="flex-1">` to preserve the flex-grow behavior for the inner content sections.

Actually, since the layout already wraps content in `<main class="flex-1">`, the individual pages should NOT have their own `<main>` tags. Update each page:

- **Landing page:** Replace `<main class="flex-1">...</main>` with just the inner content (the sections). Remove the `<main>` wrapper entirely -- the content sections are direct children of the router outlet inside the layout's `<main>`.
- **Pricing page:** Replace `<main class="flex-1">...</main>` with just the inner content sections.
- **Subscribe page:** Replace `<main class="flex-1 bg-gray-50 py-12 dark:bg-slate-900 sm:py-16">...</main>` with `<div class="bg-gray-50 py-12 dark:bg-slate-900 sm:py-16">...</div>` (keep the styling but use a `<div>` since it is inside the layout `<main>`).
- **Confirmation page:** Replace `<main class="bg-gray-50 py-16 dark:bg-slate-900 sm:py-24">...</main>` with `<div class="bg-gray-50 py-16 dark:bg-slate-900 sm:py-24">...</div>`.

**Component TypeScript changes:**

For each page component `.ts` file:
- Remove `HeaderComponent` and `FooterComponent` from the `imports` array
- Remove the import statement for `FooterComponent, HeaderComponent` from `'../../ui'` (if no other symbols are imported from `'../../ui'`, remove the import line entirely; if other symbols like `HeroSectionComponent` remain, keep the import but remove the two symbols)

Files to update:
- `landing-page.component.ts` -- still imports `HeroSectionComponent`, `FeatureCardComponent` from `'../../ui'`, so keep the import but remove `HeaderComponent`, `FooterComponent`
- `pricing-page.component.ts` -- check what other UI components it imports from `'../../ui'` and adjust accordingly
- `subscribe-page.component.ts` -- same
- `confirmation-page.component.ts` -- imports `FooterComponent, HeaderComponent` from `'../../ui'`; if those are the only imports, remove the entire import line

**Update existing unit tests:**

After removing header/footer from individual page components, existing unit tests that assert on `<chairly-web-header>` or `<chairly-web-footer>` within those page component specs must be updated or removed. The header/footer rendering is now the responsibility of `WebsiteLayoutComponent` (tested in F1).

Check each page component's `.spec.ts` file and:
1. Remove `HeaderComponent` and `FooterComponent` from the `TestBed.configureTestingModule` `imports` array (these components are no longer in the page template, so providing them in the test module is unnecessary and misleading).
2. Remove the corresponding TypeScript import statements for `HeaderComponent` and `FooterComponent` at the top of each `.spec.ts` file.
3. Remove any assertions about header/footer presence, or update them to verify the elements are NOT present (confirming the cleanup).

### F3 — Playwright e2e tests for footer positioning

Add Playwright e2e tests that verify the footer is always at or below the bottom of the viewport on all four website pages, providing regression coverage.

**File:** `apps/chairly-website-e2e/src/footer-sticky.spec.ts`

**Test structure:**

```typescript
import { expect, test } from '@playwright/test';

const pages = [
  { name: 'Landing', path: '/' },
  { name: 'Prijzen', path: '/prijzen' },
  { name: 'Abonneren', path: '/abonneren' },
  { name: 'Bevestiging', path: '/bevestiging' },
];

test.describe('Footer sticky positioning', () => {
  for (const { name, path } of pages) {
    test(`${name} page: footer should be at or below viewport bottom`, async ({ page }) => {
      // Mock API calls as needed (plans API for prijzen/abonneren pages)
      await page.route('**/api/onboarding/plans', (route) =>
        route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify([]),
        }),
      );

      await page.goto(path);
      await page.waitForLoadState('networkidle');

      const footer = page.locator('chairly-web-footer');
      await expect(footer).toBeVisible();

      // Scroll the footer into view first — boundingBox() returns null for
      // elements outside the viewport (e.g. on long pages where the footer
      // is below the fold).
      await footer.scrollIntoViewIfNeeded();

      const footerBox = await footer.boundingBox();
      const viewportSize = page.viewportSize();

      // The bottom edge of the footer should be at or below the viewport bottom
      expect(footerBox).not.toBeNull();
      expect(viewportSize).not.toBeNull();
      expect(footerBox!.y + footerBox!.height).toBeGreaterThanOrEqual(viewportSize!.height);
    });
  }
});
```

**Key points:**
- Tests run against all four website pages: `/`, `/prijzen`, `/abonneren`, `/bevestiging`
- Mock the plans API to avoid backend dependency
- For each page, verify the footer's bottom edge is at or below the viewport bottom (meaning the footer is not floating in the middle of the page)
- The test uses `boundingBox()` to get the footer's position and compares against `viewportSize()` height
- If a page has enough content to scroll, the footer will naturally be below the viewport -- that is correct behavior

**Update existing e2e tests if needed:**
- If any existing tests in `landing-page.spec.ts`, `pricing.spec.ts`, or `subscribe.spec.ts` assert on footer presence or position, verify they still pass after the layout refactor

## Acceptance Criteria

- [ ] A `WebsiteLayoutComponent` exists in `libs/website/src/lib/onboarding/ui/website-layout/`
- [ ] The component uses `min-h-screen` + flex column layout with header, main (flex-1), and footer
- [ ] All four onboarding routes are children of the layout route
- [ ] No page component contains `<chairly-web-header />` or `<chairly-web-footer />` in its template
- [ ] No page component imports `HeaderComponent` or `FooterComponent`
- [ ] No page component has a top-level `<main>` wrapper in its template (the layout provides it)
- [ ] The footer is visually at the bottom of the viewport on the confirmation page (short content)
- [ ] The footer is below the fold on pages with long content (landing, pricing)
- [ ] Unit tests for `WebsiteLayoutComponent` pass
- [ ] Existing unit tests for all four page components still pass
- [ ] Playwright e2e tests verify footer positioning on all four pages
- [ ] All existing e2e tests still pass
- [ ] All frontend quality checks pass (`npx nx affected -t lint --base=main`, `npx nx format:check --base=main`, `npx nx affected -t test --base=main`, `npx nx affected -t build --base=main`)

## Out of Scope

- Dark mode styling changes (existing dark mode support is preserved as-is)
- Header or footer content changes
- Backend changes (this is frontend-only)
- Other Angular apps (this only affects `chairly-website`)
- Responsive/mobile layout adjustments beyond what the sticky footer already provides
