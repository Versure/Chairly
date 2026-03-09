# E2E Test Coverage

## Overview

Not every domain has e2e tests covering all user interactions. Currently, services, staff, and clients have tests, but bookings has none. Additionally, some existing tests do not cover all interaction paths (e.g. empty states, error states, toggle flows). This spec adds comprehensive e2e test coverage for all domains. Fixes GitHub issue #10.

**Note:** This spec should be implemented AFTER the e2e-infrastructure spec (fixtures.ts pattern) and after the bookings feature is implemented. If bookings is not yet implemented, skip the bookings tests (F4).

## Domain Context

- Bounded context: All domains (Services, Staff, Clients, Bookings, Shared/Navigation)
- Key entities involved: Service, ServiceCategory, StaffMember, Client, Booking
- Ubiquitous language: Booking (never "appointment"), Client (never "customer"), Staff Member (never "employee"), Service
- Existing e2e test files:
  - `apps/chairly-e2e/src/services.spec.ts` -- 7 tests (CRUD + categories)
  - `apps/chairly-e2e/src/service-catalog.spec.ts` -- 5 tests (catalog view)
  - `apps/chairly-e2e/src/staff.spec.ts` -- 7 tests (CRUD + activate/deactivate)
  - `apps/chairly-e2e/src/clients.spec.ts` -- 7 tests (CRUD + nav link)
  - `apps/chairly-e2e/src/navigation.spec.ts` -- 6 tests (collapsible sidebar)
  - `apps/chairly-e2e/src/example.spec.ts` -- 1 smoke test
  - `apps/chairly-e2e/src/fixtures.ts` -- shared test fixture with global API fallback route

## Frontend Tasks

### F1 -- Add missing e2e scenarios for services domain

Review and extend `apps/chairly-e2e/src/services.spec.ts` to cover missing interaction paths. All new tests must use the shared fixtures pattern.

**Import:** `import { expect, test } from './fixtures';`

**Scenarios to add:**

1. **Empty state:** When no services exist (mock `GET /api/services` to return `[]`), the table shows the empty-state text: `Nog geen diensten. Klik op "Dienst toevoegen" om te beginnen.`
   - Setup: mock `**/api/services` GET to return `[]`, mock `**/api/service-categories` GET to return `[]`
   - Navigate to `/diensten`
   - Assert the empty-state text is visible in the table

2. **Toggle active/inactive:** Clicking the "Deactiveren" button on an active service calls `PATCH /api/services/{id}/toggle-active` and the status badge changes from "Actief" to "Inactief".
   - Setup: mock GET to return one active service, mock PATCH to return 204
   - Click the button with title "Status wijzigen"
   - Assert "Inactief" badge appears
   - Note: check the actual toggle mechanism in the codebase. The button text is `service.isActive ? 'Deactiveren' : 'Activeren'` and emits `toggleActiveClicked`. The button title is "Status wijzigen".

3. **Edit pre-fills all fields and saves changes:** Extend the existing edit test to verify all fields (Naam, Omschrijving, Duur, Prijs, Categorie) are pre-filled and that changing a value and clicking Opslaan calls PUT /api/services/{id}.
   - Setup: mock with a service that has `description`, `categoryId`, and `categoryName` set
   - Click "Dienst bewerken" button
   - Verify Naam has value "Herenknippen", Prijs has value "25", Duur (minuten) has value "30"
   - Change Naam to "Herenknippen Kort", click Opslaan
   - Assert PUT was called and new name appears in table

4. **Category assignment:** Creating a service with a category shows the category name in the table row.
   - Setup: mock categories with one category "Knippen"
   - Open the add dialog, fill in fields, select category "Knippen"
   - Click Opslaan, mock POST to return the service with `categoryName: 'Knippen'`
   - Assert "Knippen" appears in the table row

5. **Drag-and-drop reorder:** Mark as `test.skip` with a comment explaining that HTML5 drag-and-drop is not reliably testable in Playwright across all browsers. The service table uses `draggable="true"` with `(dragstart)`, `(dragover)`, `(drop)` handlers.

**Mock data pattern:** Follow the existing `mockService` and `mockCategory` objects already in the file. Use the `setupApiMocks()` helper where possible, or create per-test route mocks for tests that need different behavior.

**Test naming:** Follow the existing pattern of descriptive test names that explain what the user does and what they should see.

### F2 -- Add missing e2e scenarios for staff domain

Review and extend `apps/chairly-e2e/src/staff.spec.ts` to cover missing interaction paths.

**Import:** `import { expect, test } from './fixtures';`

**Scenarios to add:**

1. **Empty state:** When no staff members exist (mock `GET /api/staff` to return `[]`), the table shows: `Geen medewerkers gevonden`
   - Setup: mock `**/api/staff` GET to return `[]`
   - Navigate to `/medewerkers`
   - Assert the empty-state text is visible

2. **Full deactivate and reactivate cycle:** Deactivate a staff member, verify "Inactief" badge, then reactivate and verify "Actief" badge returns.
   - Setup: mock GET to return one active staff member
   - Mock `PATCH /api/staff/staff-1/deactivate` to return 204
   - Click deactivate button (title="Medewerker deactiveren"), confirm in dialog
   - Assert "Inactief" badge appears and the "Activeren" button appears (title="Medewerker activeren")
   - Mock `PATCH /api/staff/staff-1/reactivate` to return 204
   - Click "Activeren" button
   - Assert "Actief" badge returns

3. **Staff form validation:** Open the add form, immediately click Opslaan without filling required fields, verify the form does not submit (dialog stays open). The form has `Validators.required` on firstName, lastName, and role.
   - Setup: mock API
   - Click "+ Medewerker toevoegen"
   - Clear any default values, click Opslaan
   - Assert dialog is still open (form did not submit because `this.form.invalid` returns early)

4. **Staff avatar displays initials:** The existing mock staff member "Jan Jansen" should show an avatar with initials "JJ". Verify the `chairly-staff-avatar` component displays the correct initials.
   - Setup: mock with the existing `mockStaffMember` (firstName: "Jan", lastName: "Jansen")
   - Navigate to `/medewerkers`
   - Assert the avatar element contains "JJ"

**Mock data pattern:** Use the existing `mockStaffMember` object. For the reactivate flow, update the mock response to reflect the `isActive: false` state after deactivation.

### F3 -- Add missing e2e scenarios for clients domain

Review and extend `apps/chairly-e2e/src/clients.spec.ts` to cover missing interaction paths.

**Import:** `import { expect, test } from './fixtures';`

**Scenarios to add:**

1. **Empty state:** When no clients exist (mock `GET /api/clients` to return `[]`), the table shows: `Geen klanten gevonden`
   - Setup: mock `**/api/clients` GET to return `[]`
   - Navigate to `/klanten`
   - Assert the empty-state text is visible

2. **Client with only name (no email/phone):** Creating a client with only Voornaam and Achternaam (no email, no phone) should succeed. The table shows dashes for missing contact info.
   - Setup: mock POST to return a client with `email: null`, `phoneNumber: null`
   - Open add dialog, fill Voornaam and Achternaam only, click Opslaan
   - Assert the new client appears in the table

3. **Delete cancellation:** Clicking "Klant verwijderen" opens the confirmation dialog. Clicking "Annuleren" (or pressing Escape) in the confirmation dialog should close it without deleting the client.
   - Setup: mock API with one client
   - Click delete button (title="Klant verwijderen")
   - Assert confirmation dialog is visible
   - Press Escape (cross-browser reliable way to dismiss `showModal()` dialogs)
   - Assert client is still visible in the table and DELETE was NOT called

4. **Edit dialog pre-fills all fields including optional ones:** Open the edit dialog for a client that has email, phoneNumber, and notes set. Verify all fields are pre-filled.
   - Setup: mock with a client that has `email: 'anna@example.com'`, `phoneNumber: '0612345678'`, `notes: 'Vaste klant'`
   - Click edit button (title="Klant bewerken")
   - Assert Voornaam has "Anna", Achternaam has "Bakker", E-mailadres has "anna@example.com", Telefoonnummer has "0612345678", Notities has "Vaste klant"
   - Close dialog with Escape

**Mock data pattern:** Extend the existing `mockClient` object. For the edit pre-fill test, create a separate mock with all optional fields populated.

### F4 -- Add bookings e2e tests (if bookings feature exists)

**Prerequisite:** The bookings feature must be implemented first. Check whether the route `/boekingen` exists. If it does not, **skip this entire task** by marking the test file with `test.describe.skip` and a comment explaining the prerequisite.

**File:** Create `apps/chairly-e2e/src/bookings.spec.ts`

**Import:** `import { expect, test } from './fixtures';`

**Mock data:**
```typescript
const mockClient = {
  id: 'client-1',
  firstName: 'Anna',
  lastName: 'Bakker',
};

const mockStaffMember = {
  id: 'staff-1',
  firstName: 'Jan',
  lastName: 'Jansen',
};

const mockService = {
  id: 'svc-1',
  name: 'Herenknippen',
  duration: '00:30:00',
  price: 25,
};

const mockBooking = {
  id: 'booking-1',
  clientId: 'client-1',
  clientName: 'Anna Bakker',
  staffMemberId: 'staff-1',
  staffMemberName: 'Jan Jansen',
  startTime: '2026-03-10T10:00:00Z',
  endTime: '2026-03-10T10:30:00Z',
  notes: null,
  services: [{ serviceId: 'svc-1', serviceName: 'Herenknippen', duration: '00:30:00', price: 25, sortOrder: 0 }],
  createdAtUtc: '2026-03-09T00:00:00Z',
  createdBy: 'system',
  confirmedAtUtc: null,
  startedAtUtc: null,
  completedAtUtc: null,
  cancelledAtUtc: null,
};
```

**Scenarios:**

1. **Bookings list page loads:** Navigate to `/boekingen`, verify heading "Boekingen" is visible.
2. **"Nieuwe boeking" button opens create dialog:** Click the button, verify `dialog[open]` is visible.
3. **Creating a booking:** Fill in client (dropdown/autocomplete), staff member, service, date/time. Click "Opslaan". Mock POST to return the new booking. Verify the new booking appears in the table/list.
4. **Status: Bevestigen:** Click "Bevestigen" action on a booking row. Mock PATCH to return updated booking with `confirmedAtUtc` set. Verify status badge shows "Bevestigd".
5. **Status: Starten:** Click "Starten" action. Mock PATCH. Verify badge shows "Bezig".
6. **Status: Voltooien:** Click "Voltooien" action. Mock PATCH. Verify badge shows "Voltooid".
7. **Status: Annuleren:** Click "Annuleren" action. Mock PATCH. Verify badge shows "Geannuleerd".
8. **Edit pre-fill:** Click on a booking row or edit button. Verify dialog opens with pre-filled values (client name, staff member, services, date/time).
9. **Filter by date/staff:** If filtering controls exist, test selecting a date range or staff member and verify the list updates.

**Note:** Since the bookings feature does not exist yet (no `/boekingen` route found), the implementation agent should wrap all tests in `test.describe.skip('Boekingen (pending bookings feature)', ...)` and add a comment explaining the prerequisite. The mock data shapes above are based on the domain model and should be refined once the bookings API contract is defined.

Mock all API calls using `page.route()`.

### F5 -- Add navigation and cross-cutting e2e tests

The file `apps/chairly-e2e/src/navigation.spec.ts` already exists with 6 tests for the collapsible sidebar (from the collapsible-menu spec). Extend it with additional cross-cutting scenarios. If a test already exists in the file, do not duplicate it.

**Import:** `import { expect, test } from './fixtures';`

**Scenarios to add (check for duplicates before adding):**

1. **Default redirect:** Navigating to `/` redirects to `/diensten`. (This may already be covered by `example.spec.ts` -- only add if not already in `navigation.spec.ts`.)
   - Mock services and categories APIs to return empty arrays
   - Navigate to `/`
   - Assert URL contains `/diensten`

2. **All nav links navigate to correct pages:** For each link (Diensten, Klanten, Medewerkers), click the link and verify the URL changes.
   - Mock all domain APIs to return empty arrays
   - Start at `/diensten`
   - Click "Klanten" link, assert URL is `/klanten`
   - Click "Medewerkers" link, assert URL is `/medewerkers`
   - Click "Diensten" link, assert URL is `/diensten`

3. **Each page shows correct heading:** Navigate to each page and verify the `<h1>` heading text.
   - `/diensten` shows "Diensten"
   - `/klanten` shows "Klanten"
   - `/medewerkers` shows "Medewerkers"

4. **Theme toggle switches between light and dark mode:** Click the theme toggle button. Verify `data-theme="dark"` is set on the `<html>` element. Click again. Verify `data-theme` is removed or set to "light".
   - The theme toggle button has `aria-label` "Schakel naar donker thema" (when light) or "Schakel naar licht thema" (when dark)
   - After clicking, verify `document.documentElement.getAttribute('data-theme')` changes

5. **Active nav link is highlighted:** When on `/diensten`, the "Diensten" link should have the `bg-primary-600` class (from `routerLinkActive="bg-primary-600"`). When navigating to `/klanten`, the "Klanten" link should have it instead.

**Mock setup:** All navigation tests need mocks for all domain APIs (`/api/services`, `/api/service-categories`, `/api/staff`, `/api/clients`) returning empty arrays, since navigating between pages triggers API calls.

## Acceptance Criteria

- [ ] Every domain has comprehensive e2e tests covering CRUD, empty states, and toggle/status flows
- [ ] All e2e tests use the shared fixtures pattern (`import { expect, test } from './fixtures';` -- no bare `@playwright/test` imports)
- [ ] All e2e tests mock API calls using `page.route()` (no ECONNREFUSED errors)
- [ ] Empty states are tested for services ("Nog geen diensten..."), staff ("Geen medewerkers gevonden"), and clients ("Geen klanten gevonden")
- [ ] Toggle active/inactive is tested for services and staff (deactivate + reactivate cycle)
- [ ] Form validation is tested (required fields prevent submission)
- [ ] Edit dialogs are tested for pre-filling all fields (including optional ones)
- [ ] Delete cancellation is tested (Escape dismisses confirmation without deleting)
- [ ] Navigation between pages is tested with URL and heading assertions
- [ ] Theme toggle is tested (light/dark mode switch via `data-theme` attribute)
- [ ] Active nav link highlighting is tested
- [ ] Bookings tests are either implemented (if feature exists) or wrapped in `test.describe.skip`
- [ ] All frontend quality checks pass (`npx nx affected -t lint --base=main`, `npx nx format:check --base=main`, `npx nx affected -t test --base=main`, `npx nx affected -t build --base=main`)
- [ ] All e2e tests pass on Chromium, Firefox, and WebKit

## Out of Scope

- Visual regression testing (screenshot comparison)
- Performance testing
- Accessibility testing (a11y audits)
- Mobile viewport tests (covered by the collapsible-menu spec)
- Backend tests (this spec is frontend-only e2e)
