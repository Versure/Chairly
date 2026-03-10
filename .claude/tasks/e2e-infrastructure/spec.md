# E2E Test Infrastructure Fixes

## Overview

The Playwright e2e test infrastructure has two issues: (1) there is no UI mode configured for interactive debugging (#11), and (2) tests that navigate without complete API mocking produce ECONNREFUSED errors because the Vite dev server has no proxy for `/api` routes (#12). Both issues are frontend-only.

## Domain Context

- Bounded context: Shared / Infrastructure
- Key files: `apps/chairly-e2e/playwright.config.ts`, `apps/chairly/vite.config.mts`, `apps/chairly-e2e/project.json`
- All existing e2e tests use `page.route()` to mock API calls at the Playwright level — no real backend is expected during e2e runs

## Frontend Tasks

### F1 — Add Playwright UI mode script

Add an Nx target or npm script that launches Playwright in UI mode (`--ui` flag) for interactive test debugging.

**What to do:**
1. In `apps/chairly-e2e/project.json`, add a new target `e2e:ui` (or modify the existing setup) so developers can run: `npx nx run chairly-e2e:e2e --ui`
2. Verify the Playwright config already supports this (it should — `--ui` is a built-in Playwright flag that `nxE2EPreset` passes through)
3. If `nxE2EPreset` does not forward the `--ui` flag, add a separate target:
   ```json
   "e2e-ui": {
     "executor": "nx:run-commands",
     "options": {
       "command": "npx playwright test --ui",
       "cwd": "apps/chairly-e2e"
     }
   }
   ```
4. Test that `npx nx run chairly-e2e:e2e-ui` opens the Playwright UI panel

### F2 — Add a global API fallback route to prevent ECONNREFUSED

Tests that miss a `page.route()` mock for an API endpoint currently crash with ECONNREFUSED because the Vite dev server has no `/api` proxy. Fix this by adding a **global Playwright fixture** that intercepts any un-mocked `/api/**` request and returns a helpful error response instead of letting it hit a non-existent backend.

**What to do:**
1. Create a shared test fixture file: `apps/chairly-e2e/src/fixtures.ts`
2. Define a custom `test` export that extends Playwright's `test` with a global `page` hook:
   ```typescript
   import { test as base } from '@playwright/test';

   export const test = base.extend({
     page: async ({ page }, use) => {
       // Catch any un-mocked /api/ calls and return a clear error
       await page.route('**/api/**', (route) => {
         const url = route.request().url();
         console.warn(`[e2e] Un-mocked API call intercepted: ${route.request().method()} ${url}`);
         return route.fulfill({
           status: 599,
           contentType: 'application/json',
           body: JSON.stringify({ error: `Un-mocked API route: ${url}` }),
         });
       });
       await use(page);
     },
   });

   export { expect } from '@playwright/test';
   ```
3. Update all existing e2e test files (`services.spec.ts`, `staff.spec.ts`, `clients.spec.ts`, `service-catalog.spec.ts`, `example.spec.ts`) to import from `./fixtures` instead of `@playwright/test`:
   ```typescript
   import { expect, test } from './fixtures';
   ```
4. The specific `page.route()` mocks in each test will take priority over the global fallback (Playwright matches more-specific routes first, and specific `page.route` calls registered after the fixture still override).
5. Verify tests still pass by running: `cd src/frontend/chairly && npx nx run chairly-e2e:e2e --project=chromium`

### F3 — Ensure service-catalog.spec.ts has complete API mocks

The `service-catalog.spec.ts` file has some tests that navigate without setting up API mocks. Verify all tests in this file call `setupApiMocks(page)` before navigating. Fix any tests that don't.

**What to do:**
1. Read `apps/chairly-e2e/src/service-catalog.spec.ts`
2. Ensure every `test(...)` block calls the mock setup function before `page.goto()`
3. If a shared `setupApiMocks` function doesn't exist in this file, create one following the same pattern as `services.spec.ts`

## Acceptance Criteria

- [ ] `npx nx run chairly-e2e:e2e-ui` opens the Playwright UI for interactive test debugging
- [ ] No ECONNREFUSED errors appear when running e2e tests
- [ ] All e2e tests import from the shared `fixtures.ts` file
- [ ] Un-mocked API calls return a clear error (status 599) instead of crashing
- [ ] All existing e2e tests still pass
- [ ] All frontend quality checks pass (lint, test, build)

## Out of Scope

- Adding a real API proxy to Vite (no backend runs during e2e)
- Writing new e2e tests for missing domains (covered by separate issue #10)
