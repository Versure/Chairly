# PRD: Staff Management

## Introduction

Add staff management to Chairly so salon owners and managers can maintain a list of staff members, configure their roles, assign a visual identity (color + initials or photo), and define their weekly shift schedule. This is a prerequisite for the bookings feature, which needs to know which staff member performs which appointment.

Scope: **Frontend only** with a mocked API. Backend implementation follows later. Follows the same patterns established by the service catalog feature.

---

## Goals

- Allow creating, editing, and deactivating staff members
- Support two assignable roles: Manager and Staff Member (Owner role is implicit for the account creator and is not assignable here)
- Give each staff member a visual identity: a color from a fixed palette with initials as default, with optional photo upload
- Allow defining a weekly shift schedule: per day of the week, zero or more time blocks (e.g. Mon 09:00–12:00 and 13:00–17:00)
- Integrate the Staff nav item into the existing shell sidebar
- Follow the same architectural pattern as the service catalog (API service → NgRx SignalStore → smart container + dumb UI components → Playwright E2E tests)

---

## User Stories

### SM-001: Staff models and mock API service

**Description:** As a developer, I need TypeScript interfaces and a mock-capable API service for staff so that the frontend can work without a real backend.

**Acceptance Criteria:**
- [ ] `libs/chairly/src/lib/staff/models/staff.models.ts` defines:
  - `StaffRole` union type: `'manager' | 'staff_member'`
  - `ShiftBlock` interface: `{ startTime: string; endTime: string }` (ISO time strings, e.g. `'09:00'`)
  - `WeeklySchedule` interface: record of day → shift blocks: `{ [day in DayOfWeek]?: ShiftBlock[] }` where `DayOfWeek = 'monday' | 'tuesday' | 'wednesday' | 'thursday' | 'friday' | 'saturday' | 'sunday'`
  - `StaffMemberResponse` interface: `{ id: string; firstName: string; lastName: string; role: StaffRole; color: string; photoUrl: string | null; isActive: boolean; schedule: WeeklySchedule; createdAtUtc: string; updatedAtUtc: string | null }`
  - `CreateStaffMemberRequest` interface: `{ firstName: string; lastName: string; role: StaffRole; color: string; photoUrl: string | null; schedule: WeeklySchedule }`
  - `UpdateStaffMemberRequest`: same shape as `CreateStaffMemberRequest`
- [ ] `libs/chairly/src/lib/staff/models/index.ts` exports all of the above
- [ ] `libs/chairly/src/lib/staff/data-access/staff-api.service.ts` created with `StaffApiService` injectable class:
  - `getAll(): Observable<StaffMemberResponse[]>`
  - `create(request: CreateStaffMemberRequest): Observable<StaffMemberResponse>`
  - `update(id: string, request: UpdateStaffMemberRequest): Observable<StaffMemberResponse>`
  - `deactivate(id: string): Observable<void>`
  - `reactivate(id: string): Observable<void>`
  - Uses `HttpClient`, base URL `/api/staff`
- [ ] `libs/chairly/src/lib/staff/data-access/staff-api.service.spec.ts` with unit tests for each method (verify correct HTTP verb and URL)
- [ ] `libs/chairly/src/lib/staff/data-access/index.ts` exports service
- [ ] `.gitkeep` files removed from `staff/models/`, `staff/data-access/`
- [ ] `npx nx affected -t lint,test,build --base=main` passes

### SM-002: NgRx SignalStore for staff state

**Description:** As a developer, I need a SignalStore that manages the staff list so that components have a single source of truth with loading and error state.

**Acceptance Criteria:**
- [ ] `libs/chairly/src/lib/staff/data-access/staff.store.ts` created with `StaffStore` using NgRx SignalStore:
  - State: `{ staffMembers: StaffMemberResponse[]; isLoading: boolean; error: string | null }`
  - Methods: `loadAll()`, `addStaffMember(member)`, `updateStaffMember(member)`, `deactivateStaffMember(id)`, `reactivateStaffMember(id)`
  - Follows the same pattern as `ServiceStore` in `libs/chairly/src/lib/services/data-access/service.store.ts`
- [ ] `libs/chairly/src/lib/staff/data-access/staff.store.spec.ts` with unit tests covering: initial state, `loadAll` sets members, `addStaffMember` appends, `updateStaffMember` replaces by id, `deactivateStaffMember` sets `isActive: false`, `reactivateStaffMember` sets `isActive: true`
- [ ] `staff.store.ts` exported from `libs/chairly/src/lib/staff/data-access/index.ts`
- [ ] `npx nx affected -t lint,test,build --base=main` passes

### SM-003: Staff avatar component

**Description:** As a user, I want to see a visual avatar for each staff member so I can quickly identify them.

**Acceptance Criteria:**
- [ ] `libs/chairly/src/lib/staff/ui/staff-avatar.component.ts` and `.html` created:
  - Selector: `chairly-staff-avatar`
  - Inputs: `color: string` (hex or Tailwind color token), `initials: string` (2 chars), `photoUrl: string | null`, `size: 'sm' | 'md' | 'lg'` (default `'md'`)
  - If `photoUrl` is non-null: renders a circular `<img>` with the photo
  - If `photoUrl` is null: renders a colored circle (`background-color` from `color`) with `initials` centered in white text
  - Sizes: `sm` = 32px, `md` = 40px, `lg` = 56px
  - OnPush, standalone, `templateUrl:`
- [ ] Unit tests in `staff-avatar.component.spec.ts`: renders initials when no photo, renders img when photoUrl provided, applies correct size class
- [ ] Exported from `libs/chairly/src/lib/staff/ui/index.ts`
- [ ] `.gitkeep` removed from `staff/ui/`
- [ ] `npx nx affected -t lint,test,build --base=main` passes

### SM-004: Staff table component

**Description:** As an owner or manager, I want to see all staff members in a table so I can get an overview and take actions on each.

**Acceptance Criteria:**
- [ ] `libs/chairly/src/lib/staff/ui/staff-table.component.ts` and `.html` created:
  - Selector: `chairly-staff-table`
  - Inputs: `staffMembers: StaffMemberResponse[]`
  - Outputs: `edit: OutputEmitterRef<StaffMemberResponse>`, `deactivate: OutputEmitterRef<StaffMemberResponse>`, `reactivate: OutputEmitterRef<StaffMemberResponse>`
  - Table columns: Avatar (uses `chairly-staff-avatar`), Naam (firstName + lastName), Rol (Dutch label: 'Manager' / 'Medewerker'), Status (badge: 'Actief' green / 'Inactief' gray), Acties
  - Acties column: 'Bewerken' button (primary), 'Deactiveren' button (yellow, only shown when `isActive`), 'Activeren' button (green, only shown when `!isActive`)
  - Inactive rows are visually dimmed (`opacity-60`)
  - Empty state: "Geen medewerkers gevonden" message
  - OnPush, standalone, `templateUrl:`
- [ ] Unit tests in `staff-table.component.spec.ts`: renders staff members, shows correct role labels, shows Deactiveren/Activeren based on isActive, emits correct events on button click
- [ ] Exported from `libs/chairly/src/lib/staff/ui/index.ts`
- [ ] `npx nx affected -t lint,test,build --base=main` passes
- [ ] Verify in browser using dev-browser skill

### SM-005: Staff form dialog — basic info

**Description:** As an owner or manager, I want to create or edit a staff member's name, role, and color so that I can maintain an accurate team list.

**Acceptance Criteria:**
- [ ] `libs/chairly/src/lib/staff/ui/staff-form-dialog.component.ts` and `.html` created:
  - Selector: `chairly-staff-form-dialog`
  - Input: `staffMember: StaffMemberResponse | null` (null = create mode)
  - Outputs: `save: OutputEmitterRef<CreateStaffMemberRequest | UpdateStaffMemberRequest>`, `cancel: OutputEmitterRef<void>`
  - Form fields (all Dutch labels):
    - Voornaam (text, required, max 100 chars)
    - Achternaam (text, required, max 100 chars)
    - Rol (select: 'Manager' / 'Medewerker', required)
    - Kleur (color picker: fixed palette of 10 colors shown as clickable circles — see FR-3)
    - Foto URL (optional text input, placeholder: 'https://...')
  - Dialog title: 'Medewerker toevoegen' (create) / 'Medewerker bewerken' (edit)
  - Uses `<dialog>` with `showModal()`, full-screen overlay (same pattern as service-form-dialog)
  - Body scroll locked via `DOCUMENT` injection while open
  - Buttons: 'Opslaan' (primary, disabled when form invalid), 'Annuleren'
  - Pre-fills form when editing
  - Reactive `FormGroup` with typed controls
  - OnPush, standalone, `templateUrl:`
- [ ] Unit tests: create mode shows empty form, edit mode pre-fills values, Opslaan disabled when invalid, save event emits correct payload, cancel event emits on Annuleren
- [ ] Exported from `libs/chairly/src/lib/staff/ui/index.ts`
- [ ] `npx nx affected -t lint,test,build --base=main` passes
- [ ] Verify in browser using dev-browser skill

### SM-006: Shift schedule editor component

**Description:** As an owner or manager, I want to define per-day shifts for a staff member so that the system knows when they are available.

**Acceptance Criteria:**
- [ ] `libs/chairly/src/lib/staff/ui/shift-schedule-editor.component.ts` and `.html` created:
  - Selector: `chairly-shift-schedule-editor`
  - Input: `schedule: WeeklySchedule`
  - Output: `scheduleChange: OutputEmitterRef<WeeklySchedule>` (two-way via `model()` or explicit emit)
  - Renders 7 rows, one per day (Maandag through Zondag)
  - Each row:
    - Day label
    - Toggle checkbox/switch: "Werkdag" — when unchecked, day has no shifts (empty array)
    - When enabled: shows existing shift blocks; each block has a start time input, end time input, and a remove (×) button
    - '+ Dienst toevoegen' button to add a new shift block for that day (default: 09:00–17:00)
  - Validation: end time must be after start time (shown inline)
  - OnPush, standalone, `templateUrl:`
- [ ] Embedded inside `staff-form-dialog` as a section below the basic info fields (with a 'Werkrooster' heading)
- [ ] Unit tests: adds shift block on click, removes shift block, emits updated schedule, validates end > start
- [ ] Exported from `libs/chairly/src/lib/staff/ui/index.ts`
- [ ] `npx nx affected -t lint,test,build --base=main` passes
- [ ] Verify in browser using dev-browser skill

### SM-007: Staff list page and routing

**Description:** As an owner or manager, I want to navigate to a staff list page so I can manage the team.

**Acceptance Criteria:**
- [ ] `libs/chairly/src/lib/staff/feature/staff-list-page/staff-list-page.component.ts` and `.html` created:
  - Smart container: injects `StaffStore`, `StaffApiService`
  - Calls `store.loadAll()` on init
  - Shows page header: 'Medewerkers' (h1) and '+ Medewerker toevoegen' button
  - Renders `<chairly-staff-table>` passing `store.staffMembers()` as input
  - Handles edit → opens `<chairly-staff-form-dialog>` in edit mode
  - Handles deactivate/reactivate → calls API then updates store; shows confirmation dialog before deactivating (reuse shared `ConfirmationDialogComponent` if available, otherwise inline confirm)
  - Handles save from dialog → calls `create` or `update` API, then `addStaffMember`/`updateStaffMember` on store
  - Shows loading indicator while `store.isLoading()` is true ('Laden...' text)
  - OnPush, standalone, `templateUrl:`
- [ ] `libs/chairly/src/lib/staff/feature/index.ts` exports `StaffListPageComponent`
- [ ] `.gitkeep` removed from `staff/feature/`
- [ ] `libs/chairly/src/lib/staff/staff.routes.ts` created at domain root:
  ```ts
  export const staffRoutes: Route[] = [
    { path: '', component: StaffListPageComponent, providers: [StaffStore, StaffApiService] }
  ];
  ```
- [ ] `apps/chairly/src/app/app.routes.ts` updated: add `{ path: 'medewerkers', loadChildren: () => import('@org/chairly-lib').then(m => m.staffRoutes) }` inside the shell children
- [ ] `libs/chairly/src/index.ts` exports `staffRoutes`
- [ ] Unit tests for `StaffListPageComponent`: renders table, opens dialog on add click, calls create on save
- [ ] `npx nx affected -t lint,test,build --base=main` passes
- [ ] Verify in browser using dev-browser skill

### SM-008: Add Medewerkers nav item to shell

**Description:** As a user, I want a 'Medewerkers' link in the sidebar so I can navigate to the staff section.

**Acceptance Criteria:**
- [ ] `libs/shared/src/lib/ui/shell/shell.component.html` updated: add nav link 'Medewerkers' pointing to `/medewerkers`, with `routerLinkActive="active"` styling, directly below the 'Diensten' link
- [ ] Active link styling matches the existing 'Diensten' link style
- [ ] `npx nx affected -t lint,test,build --base=main` passes
- [ ] Verify in browser using dev-browser skill

### SM-009: Playwright E2E tests for staff management

**Description:** As a developer, I want E2E tests for the staff pages so regressions are caught automatically.

**Acceptance Criteria:**
- [ ] `apps/chairly-e2e/src/staff.spec.ts` created with the following test cases (all using `page.route()` to mock `/api/staff`):
  1. Navigates to `/medewerkers` — shows 'Medewerkers' h1 and staff table with a mocked staff member
  2. Shows staff member's name, role label ('Medewerker'), and status badge ('Actief')
  3. Clicking '+ Medewerker toevoegen' opens the form dialog with empty fields
  4. Filling the form and clicking 'Opslaan' calls `POST /api/staff` and shows the new member in the table
  5. Clicking 'Bewerken' (title="Medewerker bewerken") opens the dialog pre-filled with the staff member's data
  6. Clicking 'Deactiveren' opens confirmation dialog; confirming calls `PATCH /api/staff/{id}/deactivate` and shows the member as 'Inactief'
  7. 'Medewerkers' nav link is visible in sidebar and navigates correctly
- [ ] `npx nx affected -t lint,build --base=main` passes (E2E tests run separately, not in CI lint/build)

---

## Functional Requirements

- **FR-1:** A staff member has: first name, last name, role (`manager` | `staff_member`), color, optional photo URL, active status, and weekly shift schedule.
- **FR-2:** Roles available in the UI: 'Manager' and 'Medewerker'. The Owner role is not assignable here.
- **FR-3:** Color picker shows exactly 10 fixed colors as clickable circles. Suggested palette (hex): `#6366f1` (indigo), `#8b5cf6` (violet), `#ec4899` (pink), `#ef4444` (red), `#f97316` (orange), `#eab308` (yellow), `#22c55e` (green), `#14b8a6` (teal), `#3b82f6` (blue), `#64748b` (slate). The selected color gets a ring/border indicator.
- **FR-4:** Photo URL is optional. When provided, it is shown as a circular avatar image. When absent, the colored circle with initials is shown.
- **FR-5:** The weekly schedule consists of 7 days. Each day can have 0, 1, or more shift blocks. A shift block has a start time and end time (24h format, e.g. `'09:00'`, `'17:00'`). End time must be after start time.
- **FR-6:** Deactivation is soft: `isActive` is set to false. Deactivated staff appear in the list with 'Inactief' badge and dimmed row, with an 'Activeren' button instead of 'Deactiveren'.
- **FR-7:** No permanent deletion of staff members.
- **FR-8:** All user-facing text must be in Dutch (Nederlands).
- **FR-9:** Dark mode must work correctly. All new components must use `dark:` Tailwind variants for any custom/brand color classes (not covered by global CSS overrides in `tailwind.css`).
- **FR-10:** All new components use OnPush change detection, standalone declaration, and `templateUrl:` (no inline templates).
- **FR-11:** No `@Input()`/`@Output()` decorators — use `input()`, `model()`, `OutputEmitterRef`.

---

## Non-Goals

- No backend implementation (API endpoints, EF Core, migrations) — this is frontend-only with mocked API responses
- No real photo upload (file picker, upload endpoint) — only a URL text field
- No Owner role assignment
- No staff member deletion
- No availability calendar view
- No integration with booking scheduling logic
- No permission enforcement in the UI (role-based visibility is a later feature)

---

## Design Considerations

- Reuse `ConfirmationDialogComponent` from `shared` for the deactivation confirmation if it exists (check `libs/shared/src/lib/ui/`)
- Follow the visual style of the service list page: same page header pattern, same table action button styles (border + colored background at rest, colored hover)
- The shift schedule editor is embedded inside the staff form dialog as a scrollable section — the dialog may need `max-h-[90vh] overflow-y-auto` to accommodate it
- Avatar size in the table: `sm` (32px); in the form dialog preview: `lg` (56px)

---

## Technical Considerations

- Follow the exact same file/folder conventions as the services domain:
  - `models/` for interfaces
  - `data-access/` for API service and store (with `index.ts` exports)
  - `ui/` for presentational components (with `index.ts` exports)
  - `feature/staff-list-page/` subfolder for the smart container
  - `staff.routes.ts` at domain root
- Remove `.gitkeep` files as real files are added to each folder
- Import path aliases: `@org/chairly-lib` for the chairly lib, `@org/shared-lib` for shared
- The proxy config already forwards `/api/*` to the backend; mocking is done via `page.route()` in Playwright and `HttpClientTestingModule` / `provideHttpClientTesting()` in unit tests
- `WeeklySchedule` uses a record type — be careful with undefined days (treat as no shifts)

---

## Success Metrics

- All 9 user stories pass their acceptance criteria
- `npx nx affected -t lint,test,build --base=main` exits 0
- Staff list page renders correctly in both light and dark mode
- E2E tests cover the full CRUD + deactivate flow

---

## Open Questions

- Should the shift schedule editor show a visual timeline preview, or is the time-input approach sufficient for now? (Assumption: time inputs are sufficient for MVP.)
- Should inactive staff members be hidden by default with a toggle to show them, or always visible? (Assumption: always visible with visual distinction.)
