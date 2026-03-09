# E2E Test Infrastructure Fixes

## Overview

The Playwright e2e test infrastructure has two issues that block developer productivity and cause flaky test runs: (1) there is no UI mode configured for interactive debugging (issue #11), and (2) tests that navigate without complete API mocking produce ECONNREFUSED errors because the Vite dev server has no proxy for `/api` routes (issue #12). Both issues are frontend-only. This spec defines the fixes for both issues plus a cleanup pass on `service-catalog.spec.ts` which lacks API mocks entirely.

## Domain Context

- Bounded context: Shared / Infrastructure (test tooling)
- Key files: `apps/chairly-e2e/playwright.config.ts`, `apps/chairly-e2e/project.json`, all `*.spec.ts` files under `apps/chairly-e2e/src/`
- All existing e2e tests use `page.route()` to mock API calls at the Playwright level -- no real backend is expected during e2e runs

## Frontend Tasks

### F1 -- Add Playwright UI mode script

Add an Nx target that launches Playwright in UI mode (`--ui` flag) for interactive test debugging.

**What to do:**

1. In `apps/chairly-e2e/project.json`, add a new target `e2e-ui` using the `nx:run-commands` executor:
   ```json
   "e2e-ui": {
     "executor": "nx:run-commands",
     "options": {
       "command": "npx playwright test --ui",
       "cwd": "apps/chairly-e2e"
     }
   }
   ```
2. The existing `playwright.config.ts` already uses `nxE2EPreset` which provides the standard Playwright configuration. The `--ui` flag is a built-in Playwright CLI flag that works without any config changes.
3. Verify that running `npx nx run chairly-e2e:e2e-ui` opens the Playwright UI panel (manual verification -- no automated test needed for this).

**Files to modify:**
- `apps/chairly-e2e/project.json` -- add `e2e-ui` target

**Acceptance:**
- `npx nx run chairly-e2e:e2e-ui` opens the Playwright interactive UI

### F2 -- Add a global API fallback route to prevent ECONNREFUSED

Tests that miss a `page.route()` mock for an API endpoint currently crash with ECONNREFUSED because the Vite dev server has no `/api` proxy and no backend is running. Fix this by adding a global Playwright fixture that intercepts any un-mocked `/api/**` request and returns a helpful error response instead of letting it hit a non-existent backend.

**What to do:**

1. Create a new shared test fixture file at `apps/chairly-e2e/src/fixtures.ts`.
2. Define a custom `test` export that extends Playwright's `base.test` with a global `page` fixture override:
   ```typescript
   import { test as base } from '@playwright/test';

   export const test = base.extend({
     page: async ({ page }, use) => {
       await page.route('**/api/**', (route) => {
         const url = route.request().url();
         // eslint-disable-next-line no-console -- e2e test warning, not production code
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
3. Update **all** existing e2e test files to import `test` and `expect` from the local `./fixtures` module instead of from `@playwright/test`:
   - `apps/chairly-e2e/src/example.spec.ts`
   - `apps/chairly-e2e/src/services.spec.ts`
   - `apps/chairly-e2e/src/service-catalog.spec.ts`
   - `apps/chairly-e2e/src/staff.spec.ts`
   - `apps/chairly-e2e/src/clients.spec.ts`

   Change:
   ```typescript
   import { expect, test } from '@playwright/test';
   ```
   To:
   ```typescript
   import { expect, test } from './fixtures';
   ```

4. Specific `page.route()` mocks registered inside individual tests take priority over the global fallback because Playwright matches the most recently registered route first. Tests that already set up their own mocks will continue to work unchanged.

**Files to create:**
- `apps/chairly-e2e/src/fixtures.ts`

**Files to modify:**
- `apps/chairly-e2e/src/example.spec.ts` -- change import
- `apps/chairly-e2e/src/services.spec.ts` -- change import
- `apps/chairly-e2e/src/service-catalog.spec.ts` -- change import
- `apps/chairly-e2e/src/staff.spec.ts` -- change import
- `apps/chairly-e2e/src/clients.spec.ts` -- change import

**Note on `console.warn`:** The `console.warn` in the fixture is acceptable because e2e test files are not production code. Add an ESLint disable comment if the linter flags it.

**Verification:**
- Run `cd src/frontend/chairly && npx nx run chairly-e2e:e2e --project=chromium` and confirm all tests pass
- No ECONNREFUSED errors in test output

### F3 -- Ensure service-catalog.spec.ts has complete API mocks

The `service-catalog.spec.ts` file has tests that navigate to `/diensten` without setting up API mocks for the `/api/services` and `/api/service-categories` endpoints. These tests rely on the global fallback from F2 to avoid ECONNREFUSED, but they should explicitly mock the API calls for reliable assertions.

**What to do:**

1. Read `apps/chairly-e2e/src/service-catalog.spec.ts` (current state: 5 tests, none call `setupApiMocks` or `page.route` before `page.goto`).
2. Add a `setupApiMocks` function to `service-catalog.spec.ts` following the same pattern used in `services.spec.ts`:
   ```typescript
   const mockCategory = {
     id: 'cat-1',
     name: 'Knippen',
     sortOrder: 0,
     createdAtUtc: '2026-01-01T00:00:00Z',
     createdBy: 'system',
   };

   const mockService = {
     id: 'svc-1',
     name: 'Herenknippen',
     description: null,
     duration: '00:30:00',
     price: 25,
     categoryId: 'cat-1',
     categoryName: 'Knippen',
     isActive: true,
     sortOrder: 0,
     createdAtUtc: '2026-01-01T00:00:00Z',
     createdBy: 'system',
     updatedAtUtc: null,
     updatedBy: null,
   };

   async function setupApiMocks(page: import('@playwright/test').Page): Promise<void> {
     await page.route('**/api/service-categories', (route) => {
       if (route.request().method() === 'GET') {
         return route.fulfill({ json: [mockCategory] });
       }
       return route.fulfill({ status: 404, body: '' });
     });
     await page.route('**/api/services', (route) => {
       if (route.request().method() === 'GET') {
         return route.fulfill({ json: [mockService] });
       }
       return route.fulfill({ status: 404, body: '' });
     });
   }
   ```
3. Add `await setupApiMocks(page);` before every `page.goto('/diensten')` call in each test.
4. The import line should already be updated to `./fixtures` as part of F2.

**Files to modify:**
- `apps/chairly-e2e/src/service-catalog.spec.ts` -- add mock data, `setupApiMocks` function, and call it in every test

**Verification:**
- All 5 tests in `service-catalog.spec.ts` pass without ECONNREFUSED
- No un-mocked API warnings appear in output for these tests

## Acceptance Criteria

- [ ] `npx nx run chairly-e2e:e2e-ui` opens the Playwright UI for interactive test debugging
- [ ] No ECONNREFUSED errors appear when running e2e tests
- [ ] All e2e test files import `test` and `expect` from the shared `./fixtures` module
- [ ] Un-mocked API calls return a clear error response (status 599) instead of crashing
- [ ] All existing e2e tests still pass (`npx nx run chairly-e2e:e2e --project=chromium`)
- [ ] All frontend quality checks pass (lint, format, test, build)

## Out of Scope

- Adding a real API proxy to the Vite dev server (no backend runs during e2e)
- Writing new e2e tests for missing domains (covered by separate issue #10)
- Backend changes (this feature is entirely frontend/test infrastructure)
