# E2E Test Coverage

## Overview

Not every domain has e2e tests covering all user interactions. Currently, services, staff, and clients have tests, but bookings has none. Additionally, some existing tests may not cover all interaction paths (e.g. empty states, error states). This spec adds comprehensive e2e test coverage for all domains. Fixes GitHub issue #10.

**Note:** This spec should be implemented AFTER the e2e-infrastructure spec (fixtures.ts pattern) and after the bookings feature is implemented. If bookings is not yet implemented, skip the bookings tests.

## Domain Context

- Bounded context: All domains
- Existing e2e test files:
  - `apps/chairly-e2e/src/services.spec.ts` — 7 tests (CRUD + categories)
  - `apps/chairly-e2e/src/service-catalog.spec.ts` — 5 tests (catalog view)
  - `apps/chairly-e2e/src/staff.spec.ts` — 7 tests (CRUD + activate/deactivate)
  - `apps/chairly-e2e/src/clients.spec.ts` — 6 tests (CRUD)
  - `apps/chairly-e2e/src/example.spec.ts` — 1 smoke test

## Frontend Tasks

### F1 — Add missing e2e scenarios for services domain

Review and extend `apps/chairly-e2e/src/services.spec.ts` to cover:

- [ ] Empty state: when no services exist, shows "Geen diensten gevonden" (or similar empty message)
- [ ] Toggling a service active/inactive updates the table row
- [ ] Editing a service pre-fills all fields correctly and saves changes
- [ ] Category assignment: creating a service with a category shows the category name
- [ ] Drag-and-drop reorder (if testable — mark as skip if not reliable in Playwright)

Use `import { expect, test } from './fixtures';` (from e2e-infrastructure spec).

### F2 — Add missing e2e scenarios for staff domain

Review and extend `apps/chairly-e2e/src/staff.spec.ts` to cover:

- [ ] Empty state: when no staff members exist, shows appropriate empty message
- [ ] Deactivate and reactivate flow (full cycle)
- [ ] Staff form validation: required fields show errors when empty
- [ ] Staff avatar displays initials correctly

Use `import { expect, test } from './fixtures';`.

### F3 — Add missing e2e scenarios for clients domain

Review and extend `apps/chairly-e2e/src/clients.spec.ts` to cover:

- [ ] Empty state: when no clients exist, shows "Geen klanten gevonden"
- [ ] Client with only name (no email/phone): should create successfully
- [ ] Delete confirmation: clicking "Annuleren" in the delete dialog cancels the deletion
- [ ] Edit dialog pre-fills all fields including optional ones

Use `import { expect, test } from './fixtures';`.

### F4 — Add bookings e2e tests (if bookings feature exists)

**Prerequisite:** The bookings feature must be implemented first. If the route `/bookings` or `/boekingen` does not exist, skip this task.

Create `apps/chairly-e2e/src/bookings.spec.ts` with:

- [ ] Bookings list page loads with heading "Boekingen"
- [ ] "Nieuwe boeking" button opens the create dialog
- [ ] Creating a booking: fill in fields, click "Opslaan", verify row appears in table
- [ ] Status action: "Bevestigen" changes status badge to "Bevestigd"
- [ ] Status action: "Starten" changes status badge to "Bezig"
- [ ] Status action: "Voltooien" changes status badge to "Voltooid"
- [ ] Status action: "Annuleren" changes status badge to "Geannuleerd"
- [ ] Editing a booking: click row, verify dialog opens with pre-filled values
- [ ] Filtering by date and staff member

Mock all API calls using `page.route()`.
Use `import { expect, test } from './fixtures';`.

### F5 — Add navigation and cross-cutting e2e tests

Create `apps/chairly-e2e/src/navigation.spec.ts` (if not already created by collapsible-menu spec) with:

- [ ] Default redirect: navigating to `/` redirects to `/diensten`
- [ ] All nav links navigate to the correct pages
- [ ] Each page shows the correct heading (Diensten, Klanten, Medewerkers, Boekingen)
- [ ] Theme toggle switches between light and dark mode
- [ ] Active nav link is highlighted

Use `import { expect, test } from './fixtures';`.

## Acceptance Criteria

- [ ] Every domain has comprehensive e2e tests covering all user interactions
- [ ] All e2e tests use the shared fixtures pattern (no bare `@playwright/test` imports)
- [ ] All e2e tests mock API calls (no ECONNREFUSED errors)
- [ ] Empty states, error states, and validation states are tested where applicable
- [ ] Navigation and theme toggle are tested
- [ ] All frontend quality checks pass (lint, test, build)
- [ ] All e2e tests pass on Chromium, Firefox, and WebKit

## Out of Scope

- Visual regression testing (screenshots comparison)
- Performance testing
- Accessibility testing (a11y audits)
- Mobile viewport tests (covered by collapsible-menu spec)
