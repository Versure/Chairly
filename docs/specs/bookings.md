# Feature: Bookings

## Context

Bookings are the core of Chairly. A booking represents a scheduled visit by a client with a staff member, containing one or more services. This spec covers both the backend API and the Angular frontend.

## User Stories

- As an owner or manager, I want to view all bookings so I can see the salon's schedule.
- As a staff member, I want to view my own bookings so I can see my upcoming appointments.
- As an owner, manager, or staff member, I want to create a booking so I can schedule a client visit.
- As an owner or manager, I want to update a booking so I can correct mistakes or reschedule.
- As an owner, manager, or staff member, I want to cancel a booking so I can handle cancellations.
- As an owner or manager, I want to confirm, start, complete, or mark a booking as no-show to track its progress through the salon.
- As a user, I want to filter bookings by date and staff member so I can find specific bookings quickly.
- As a user, I want to see the current status of each booking at a glance so I know what action (if any) is needed.

## Acceptance Criteria

**Backend:**
- [ ] GET /api/bookings returns all bookings for the current tenant, optionally filtered by date and/or staffMemberId
- [ ] GET /api/bookings/{id} returns a single booking with its services
- [ ] POST /api/bookings creates a booking, validates client/staff/services exist, checks for staff overlap, snapshots service data
- [ ] PUT /api/bookings/{id} updates a booking; blocked in terminal states
- [ ] POST /api/bookings/{id}/cancel sets CancelledAtUtc; blocked in terminal states
- [ ] POST /api/bookings/{id}/confirm sets ConfirmedAtUtc; blocked if not in Scheduled state
- [ ] POST /api/bookings/{id}/start sets StartedAtUtc; blocked if not in Scheduled or Confirmed state
- [ ] POST /api/bookings/{id}/complete sets CompletedAtUtc; blocked if not InProgress
- [ ] POST /api/bookings/{id}/no-show sets NoShowAtUtc; blocked if not in Scheduled or Confirmed state
- [ ] EndTime is always calculated as StartTime + sum of service durations
- [ ] Service name, duration, and price are snapshotted at booking creation time
- [ ] Overlap detection: a staff member cannot have two non-cancelled, non-no-show bookings with overlapping times

**Frontend:**
- [ ] Booking list page at `/bookings` shows all bookings for the tenant in a table
- [ ] Table can be filtered by date and staff member
- [ ] Each row shows: client name (placeholder until client lookup), staff member name (placeholder), start/end time, services, status badge
- [ ] Clicking "Nieuwe boeking" opens the create dialog
- [ ] Clicking a booking row opens the edit/detail dialog
- [ ] Create dialog: select date/time, client ID, staff member ID, one or more service IDs, optional notes
- [ ] Edit dialog: same fields, pre-filled; updates via PUT
- [ ] Status action buttons (Bevestigen, Starten, Voltooien, Annuleren, Niet-verschenen) shown based on current status
- [ ] Status transitions call the appropriate action endpoint and refresh the store
- [ ] All UI text is in Dutch
- [ ] Playwright e2e tests cover: list renders, create flow, status transitions

## Domain Model

**Entity: Booking (Aggregate Root)**

| Field | Type | Notes |
|---|---|---|
| `Id` | Guid | PK |
| `TenantId` | Guid | FK (cross-cutting) |
| `ClientId` | Guid | FK to Clients |
| `StaffMemberId` | Guid | FK to StaffMembers |
| `StartTime` | DateTimeOffset | required |
| `EndTime` | DateTimeOffset | calculated, required |
| `Notes` | string? | optional, max 1000 |
| `CreatedAtUtc` | DateTimeOffset | required |
| `CreatedBy` | Guid | required (Guid.Empty until auth) |
| `UpdatedAtUtc` | DateTimeOffset? | set on update |
| `UpdatedBy` | Guid? | set on update |
| `ConfirmedAtUtc` | DateTimeOffset? | set on confirm |
| `ConfirmedBy` | Guid? | set on confirm |
| `StartedAtUtc` | DateTimeOffset? | set on start |
| `StartedBy` | Guid? | set on start |
| `CompletedAtUtc` | DateTimeOffset? | set on complete |
| `CompletedBy` | Guid? | set on complete |
| `CancelledAtUtc` | DateTimeOffset? | set on cancel |
| `CancelledBy` | Guid? | set on cancel |
| `NoShowAtUtc` | DateTimeOffset? | set on no-show |
| `NoShowBy` | Guid? | set on no-show |
| `BookingServices` | List\<BookingService\> | owned collection |

**Entity: BookingService (Owned by Booking — stored in separate table "BookingServices")**

| Field | Type | Notes |
|---|---|---|
| `Id` | Guid | PK (auto-generated, not meaningful in domain) |
| `ServiceId` | Guid | reference (no FK constraint — preserves history if service deleted) |
| `ServiceName` | string | snapshot of Service.Name at booking time, max 200 |
| `Duration` | TimeSpan | snapshot of Service.Duration at booking time |
| `Price` | decimal | snapshot of Service.Price at booking time |
| `SortOrder` | int | order of services within the booking |

**Derived Status (no status column — per ADR-009):**
- **Scheduled**: only `CreatedAtUtc` is set (no other timestamps)
- **Confirmed**: `ConfirmedAtUtc` is set
- **InProgress**: `StartedAtUtc` is set
- **Completed**: `CompletedAtUtc` is set
- **Cancelled**: `CancelledAtUtc` is set
- **NoShow**: `NoShowAtUtc` is set

**Terminal states**: Completed, Cancelled, NoShow — no further transitions allowed.

**Valid state transitions:**
- Scheduled → Confirmed (confirm)
- Scheduled → InProgress (start) — skip confirm
- Scheduled → Cancelled (cancel)
- Scheduled → NoShow (no-show)
- Confirmed → InProgress (start)
- Confirmed → Cancelled (cancel)
- Confirmed → NoShow (no-show)
- InProgress → Completed (complete)
- InProgress → Cancelled (cancel) — allowed; edge case (client leaves mid-service)

## API Endpoints

| Method | Route | Description |
|--------|-------|-------------|
| GET | /api/bookings | List bookings (filtered by date and/or staffMemberId) |
| GET | /api/bookings/{id} | Get single booking |
| POST | /api/bookings | Create a booking |
| PUT | /api/bookings/{id} | Update a booking |
| POST | /api/bookings/{id}/cancel | Cancel a booking |
| POST | /api/bookings/{id}/confirm | Confirm a booking |
| POST | /api/bookings/{id}/start | Start a booking (in-progress) |
| POST | /api/bookings/{id}/complete | Complete a booking |
| POST | /api/bookings/{id}/no-show | Mark a booking as no-show |

### GET /api/bookings

Query parameters (all optional):
- `date` — ISO 8601 date string (e.g. `2026-03-15`). Filters to bookings where `StartTime.Date == date`.
- `staffMemberId` — Guid. Filters to bookings for a specific staff member.

Returns `BookingResponse[]` ordered by `StartTime` ascending.

### GET /api/bookings/{id}

Returns `BookingResponse` or 404.

### POST /api/bookings

Request body:
```json
{
  "clientId": "guid (required)",
  "staffMemberId": "guid (required)",
  "startTime": "ISO 8601 (required)",
  "serviceIds": ["guid", "guid"],
  "notes": "string | null"
}
```

Validation (in handler, not Data Annotations for cross-entity checks):
1. `serviceIds` must have at least 1 item → 400 (via ValidationException)
2. Client must exist for tenant and not be soft-deleted → 404
3. StaffMember must exist for tenant and be active (DeactivatedAtUtc is null) → 404
4. All serviceIds must exist for tenant and be active (IsActive = true) → 404
5. No overlapping booking for the staff member → 409

Overlap check: any booking for the same staff member where `CancelledAtUtc == null AND NoShowAtUtc == null AND StartTime < newEndTime AND EndTime > newStartTime`.

EndTime = StartTime + sum of all service durations.

Returns `201 Created` with `BookingResponse`.

### PUT /api/bookings/{id}

Same request body as POST. Returns `200 OK` with updated `BookingResponse`.

Returns `404` if booking not found.
Returns `409 Conflict` if booking is in a terminal state (Completed, Cancelled, NoShow).

Overlap check excludes the booking being updated (a booking does not conflict with itself).

### POST /api/bookings/{id}/cancel

No request body. Returns `204 No Content`.
Returns `404` if not found.
Returns `409 Conflict` if already in terminal state (Completed, Cancelled, NoShow).

Note: Cancelling an InProgress booking is allowed (client leaves mid-service).

### POST /api/bookings/{id}/confirm

No request body. Returns `204 No Content`.
Returns `404` if not found.
Returns `409 Conflict` if not in Scheduled state (i.e., any other timestamp is already set).

### POST /api/bookings/{id}/start

No request body. Returns `204 No Content`.
Returns `404` if not found.
Returns `409 Conflict` if not in Scheduled or Confirmed state.

### POST /api/bookings/{id}/complete

No request body. Returns `204 No Content`.
Returns `404` if not found.
Returns `409 Conflict` if not InProgress (StartedAtUtc must be set; CompletedAtUtc, CancelledAtUtc, NoShowAtUtc must be null).

### POST /api/bookings/{id}/no-show

No request body. Returns `204 No Content`.
Returns `404` if not found.
Returns `409 Conflict` if not in Scheduled or Confirmed state.

### BookingResponse

```json
{
  "id": "guid",
  "clientId": "guid",
  "staffMemberId": "guid",
  "startTime": "ISO 8601",
  "endTime": "ISO 8601",
  "notes": "string | null",
  "status": "Scheduled | Confirmed | InProgress | Completed | Cancelled | NoShow",
  "services": [
    {
      "serviceId": "guid",
      "serviceName": "string",
      "duration": "HH:MM:SS",
      "price": 0.00,
      "sortOrder": 0
    }
  ],
  "createdAtUtc": "ISO 8601",
  "updatedAtUtc": "ISO 8601 | null",
  "confirmedAtUtc": "ISO 8601 | null",
  "startedAtUtc": "ISO 8601 | null",
  "completedAtUtc": "ISO 8601 | null",
  "cancelledAtUtc": "ISO 8601 | null",
  "noShowAtUtc": "ISO 8601 | null"
}
```

**Status derivation (in priority order):**
1. `CancelledAtUtc != null` → "Cancelled"
2. `NoShowAtUtc != null` → "NoShow"
3. `CompletedAtUtc != null` → "Completed"
4. `StartedAtUtc != null` → "InProgress"
5. `ConfirmedAtUtc != null` → "Confirmed"
6. else → "Scheduled"

## Business Rules

- A booking must have at least one service
- EndTime is always StartTime + sum of all BookingService durations
- Service data (name, duration, price) is snapshotted at creation — catalog changes do not affect existing bookings
- ServiceId in BookingService has NO database FK constraint (preserves booking history if service is later deleted)
- Staff member overlap detection excludes Cancelled and NoShow bookings
- `CreatedBy`, `UpdatedBy`, etc. use `Guid.Empty` as placeholder until Keycloak auth is integrated
- BookingService.Id is a Guid assigned at creation (not domain-meaningful, EF Core key only)

## Events (async)

None in this iteration. Future: `BookingCreated`, `BookingCancelled`, `BookingCompleted` domain events for Notifications context.

---

## Frontend

### F1 — Booking models

**Folder:** `libs/chairly/src/lib/bookings/models/`

TypeScript interfaces matching the backend `BookingResponse`:

```typescript
export type BookingStatus =
  | 'Scheduled'
  | 'Confirmed'
  | 'InProgress'
  | 'Completed'
  | 'Cancelled'
  | 'NoShow';

export interface BookingServiceItem {
  serviceId: string;
  serviceName: string;
  duration: string;       // "HH:MM:SS"
  price: number;
  sortOrder: number;
}

export interface Booking {
  id: string;
  clientId: string;
  staffMemberId: string;
  startTime: string;      // ISO 8601
  endTime: string;
  notes: string | null;
  status: BookingStatus;
  services: BookingServiceItem[];
  createdAtUtc: string;
  updatedAtUtc: string | null;
  confirmedAtUtc: string | null;
  startedAtUtc: string | null;
  completedAtUtc: string | null;
  cancelledAtUtc: string | null;
  noShowAtUtc: string | null;
}

export interface CreateBookingRequest {
  clientId: string;
  staffMemberId: string;
  startTime: string;
  serviceIds: string[];
  notes: string | null;
}

export type UpdateBookingRequest = CreateBookingRequest;

export interface BookingFilter {
  date?: string;          // YYYY-MM-DD
  staffMemberId?: string;
}
```

Export everything from `models/index.ts`.

### F2 — BookingApiService

**Folder:** `libs/chairly/src/lib/bookings/data-access/`

`BookingApiService` wraps all booking endpoints:

```typescript
getBookings(filter?: BookingFilter): Observable<Booking[]>
getBooking(id: string): Observable<Booking>
createBooking(request: CreateBookingRequest): Observable<Booking>
updateBooking(id: string, request: UpdateBookingRequest): Observable<Booking>
cancelBooking(id: string): Observable<void>
confirmBooking(id: string): Observable<void>
startBooking(id: string): Observable<void>
completeBooking(id: string): Observable<void>
markNoShow(id: string): Observable<void>
```

- Inject `HttpClient`
- Base path `/api/bookings`
- `getBookings` passes `date` and `staffMemberId` as query params when present (use `HttpParams`)
- All action endpoints (`cancel`, `confirm`, etc.) are `POST` with empty body

### F3 — BookingStore

**Folder:** `libs/chairly/src/lib/bookings/data-access/`

NgRx SignalStore (`bookingStore`) with:

**State:**
```typescript
bookings: Booking[]
selectedBooking: Booking | null
loading: boolean
error: string | null
activeFilter: BookingFilter
```

**Methods:**
- `loadBookings(filter?: BookingFilter)` — sets `activeFilter`, calls `getBookings`, updates `bookings`
- `createBooking(request: CreateBookingRequest)` — calls API, then `loadBookings(activeFilter)`
- `updateBooking(id, request: UpdateBookingRequest)` — calls API, then `loadBookings(activeFilter)`
- `cancelBooking(id)` — calls API, then `loadBookings(activeFilter)`
- `confirmBooking(id)` — calls API, then `loadBookings(activeFilter)`
- `startBooking(id)` — calls API, then `loadBookings(activeFilter)`
- `completeBooking(id)` — calls API, then `loadBookings(activeFilter)`
- `markNoShow(id)` — calls API, then `loadBookings(activeFilter)`
- `selectBooking(booking: Booking | null)` — sets `selectedBooking`

Export store and service from `data-access/index.ts`.

### F4 — Booking list page

**Folder:** `libs/chairly/src/lib/bookings/feature/booking-list-page/`

Smart container component `chairly-booking-list-page`.

- `OnPush` change detection, standalone
- On init: calls `store.loadBookings()`
- Template (`booking-list-page.component.html`):
  - Page heading: **"Boekingen"**
  - Filter row: date input (type="date") + staff member ID input (placeholder "Medewerker ID"), with a **"Filteren"** button that calls `store.loadBookings(filter)`
  - **"Nieuwe boeking"** button (primary) that opens the create dialog
  - `<chairly-booking-table>` receiving `[bookings]` signal and emitting `(bookingSelected)` and `(statusAction)` events
  - `<chairly-booking-form-dialog>` controlled by `dialogOpen` and `editingBooking` signals

Signals (all `signal()`):
- `dialogOpen: boolean`
- `editingBooking: Booking | null`

When `(bookingSelected)` fires: set `editingBooking` to the booking, set `dialogOpen = true`
When `(statusAction)` fires with `{ action, bookingId }`: call the appropriate store method
When dialog closes: set `dialogOpen = false`, set `editingBooking = null`

### F5 — Booking table component

**Folder:** `libs/chairly/src/lib/bookings/ui/`

Presentational component `chairly-booking-table`.

Inputs (`input()`):
- `bookings: Booking[]`

Outputs (`OutputEmitterRef`):
- `bookingSelected` emits `Booking`
- `statusAction` emits `{ action: 'confirm' | 'start' | 'complete' | 'cancel' | 'noShow', bookingId: string }`

Template columns:
| Kolomkop | Inhoud |
|---|---|
| Datum & tijd | `startTime` formatted as `dd-MM-yyyy HH:mm` — `EndTime` formatted as `HH:mm` |
| Klant | `clientId` (placeholder — client lookup out of scope for this iteration) |
| Medewerker | `staffMemberId` (placeholder) |
| Diensten | comma-separated `serviceName` values |
| Status | badge using `status` value — Dutch labels: Gepland / Bevestigd / Bezig / Voltooid / Geannuleerd / Niet-verschenen |
| Acties | `<chairly-booking-status-actions>` |

Clicking a row body emits `bookingSelected` with that booking.

Status badge color mapping:
- Gepland → gray
- Bevestigd → blue
- Bezig → yellow/amber
- Voltooid → green
- Geannuleerd → red
- Niet-verschenen → orange

### F6 — Booking form dialog

**Folder:** `libs/chairly/src/lib/bookings/ui/`

Presentational component `chairly-booking-form-dialog`.

Inputs:
- `open: boolean` — controls dialog visibility via `showModal()` / `close()`
- `booking: Booking | null` — when set, pre-fills the form (edit mode); when null, creates new

Outputs:
- `saved` emits `{ id: string | null, request: CreateBookingRequest }` — `id` is null for create
- `cancelled` emits void

Form fields (all required unless noted):
| Veld | Type | Validatie |
|---|---|---|
| Klant ID | text | required, valid UUID format |
| Medewerker ID | text | required, valid UUID format |
| Datum & tijd | datetime-local | required |
| Dienst-IDs | text (comma-separated) | required, at least one UUID |
| Notities | textarea | optional, max 1000 chars |

Title:
- Create: **"Nieuwe boeking"**
- Edit: **"Boeking bewerken"**

Buttons: **"Opslaan"** (primary, submits form) / **"Annuleren"** (secondary, emits `cancelled`)

Use native `<dialog>` with `showModal()` per the project convention in `CLAUDE.md`.
Inject `DOCUMENT`, set `document.body.style.overflow` on open/close.

### F7 — Booking status actions component

**Folder:** `libs/chairly/src/lib/bookings/ui/`

Presentational component `chairly-booking-status-actions`.

Inputs:
- `booking: Booking`

Outputs:
- `action` emits `{ action: 'confirm' | 'start' | 'complete' | 'cancel' | 'noShow', bookingId: string }`

Renders action buttons based on `booking.status`:

| Status | Visible buttons |
|---|---|
| Scheduled | Bevestigen, Starten, Annuleren, Niet-verschenen |
| Confirmed | Starten, Annuleren, Niet-verschenen |
| InProgress | Voltooien, Annuleren |
| Completed / Cancelled / NoShow | _(no buttons)_ |

Each button emits `action` with its action key and `booking.id`.

Export all UI components from `ui/index.ts`.

### F8 — Route config and registration

**File:** `libs/chairly/src/lib/bookings/bookings.routes.ts` (at domain root)

```typescript
export const bookingsRoutes: Routes = [
  {
    path: '',
    component: BookingListPageComponent,
  },
];
```

Register in the app shell under `/bookings` (lazy-loaded):
```typescript
{ path: 'bookings', loadChildren: () => import('@org/chairly-lib').then(m => m.bookingsRoutes) }
```

Add a **"Boekingen"** navigation link in the app shell sidebar/nav.

### F9 — Playwright e2e tests

**Folder:** `apps/chairly-e2e/src/`

File: `bookings.spec.ts`

Scenarios:
1. **Lijst laadt** — navigate to `/bookings`, verify the heading "Boekingen" and table are visible
2. **Nieuwe boeking aanmaken** — click "Nieuwe boeking", fill in clientId, staffMemberId, a date/time, a serviceId (comma-separated), click "Opslaan", verify a row appears in the table
3. **Status actie: Bevestigen** — find a Scheduled booking, click "Bevestigen", verify status badge changes to "Bevestigd"
4. **Boeking bewerken** — click a row, verify the dialog opens in edit mode with pre-filled values, change notes, click "Opslaan"

Use the API directly (`request.post('/api/bookings', ...)`) to seed test data where necessary.

---

## Out of Scope

- Recurring bookings
- Multi-staff bookings
- Client self-service / online booking
- Working hours validation (staff availability check against WorkingHoursEntry)
- Payment / invoice creation on completion (Billing context, separate feature)
- Notifications (separate feature)
