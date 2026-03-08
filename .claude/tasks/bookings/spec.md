# Bookings

## Overview

Bookings are the core of Chairly. A booking represents a scheduled visit by a client with a staff member, containing one or more services. This feature covers the full CRUD lifecycle plus state transitions (confirm, start, complete, cancel, no-show) for both the backend API and the Angular frontend.

## Domain Context

- Bounded context: **Bookings**
- Key entities involved: `Booking` (aggregate root), `BookingService` (owned entity), `Client`, `StaffMember`, `Service`
- Ubiquitous language: **Booking** (never "appointment"), **Client** (never "customer"), **Staff Member** (never "employee"), **Service** (catalog offering), **No-Show** (client did not arrive)
- Status is derived from timestamp pairs per ADR-009 (no status column)

## Backend Tasks

### B1 — Booking and BookingService domain entities

Create the `Booking` and `BookingService` domain entities in `Chairly.Domain/Entities/`.

**Booking entity** (`Booking.cs`):

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | PK |
| `TenantId` | `Guid` | required |
| `ClientId` | `Guid` | FK to Clients |
| `StaffMemberId` | `Guid` | FK to StaffMembers |
| `StartTime` | `DateTimeOffset` | required |
| `EndTime` | `DateTimeOffset` | calculated, required |
| `Notes` | `string?` | optional, max 1000 |
| `CreatedAtUtc` | `DateTimeOffset` | required |
| `CreatedBy` | `Guid` | required (`Guid.Empty` until auth) |
| `UpdatedAtUtc` | `DateTimeOffset?` | set on update |
| `UpdatedBy` | `Guid?` | set on update |
| `ConfirmedAtUtc` | `DateTimeOffset?` | set on confirm |
| `ConfirmedBy` | `Guid?` | set on confirm |
| `StartedAtUtc` | `DateTimeOffset?` | set on start |
| `StartedBy` | `Guid?` | set on start |
| `CompletedAtUtc` | `DateTimeOffset?` | set on complete |
| `CompletedBy` | `Guid?` | set on complete |
| `CancelledAtUtc` | `DateTimeOffset?` | set on cancel |
| `CancelledBy` | `Guid?` | set on cancel |
| `NoShowAtUtc` | `DateTimeOffset?` | set on no-show |
| `NoShowBy` | `Guid?` | set on no-show |
| `BookingServices` | `List<BookingService>` | navigation property |

Navigation properties: `Client? Client`, `StaffMember? StaffMember`.

**BookingService entity** (`BookingService.cs`):

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | PK (auto-generated, EF key only) |
| `BookingId` | `Guid` | FK to Booking |
| `ServiceId` | `Guid` | reference (NO FK constraint) |
| `ServiceName` | `string` | snapshot, max 200 |
| `Duration` | `TimeSpan` | snapshot |
| `Price` | `decimal` | snapshot |
| `SortOrder` | `int` | order within booking |

Navigation property: `Booking? Booking`.

**Derived Status helper** — add a `BookingStatus` enum in `Chairly.Domain/Enums/BookingStatus.cs`:
```csharp
public enum BookingStatus { Scheduled, Confirmed, InProgress, Completed, Cancelled, NoShow }
```

Add a static helper method or extension method to derive status from timestamps (priority order):
1. `CancelledAtUtc != null` -> Cancelled
2. `NoShowAtUtc != null` -> NoShow
3. `CompletedAtUtc != null` -> Completed
4. `StartedAtUtc != null` -> InProgress
5. `ConfirmedAtUtc != null` -> Confirmed
6. else -> Scheduled

### B2 — EF Core configuration and migration

Create EF Core configurations and add the new DbSets.

**DbContext changes** (`ChairlyDbContext.cs`):
- Add `DbSet<Booking> Bookings`
- Add `DbSet<BookingService> BookingServices`

**BookingConfiguration** (`Chairly.Infrastructure/Persistence/Configurations/BookingConfiguration.cs`):
- `builder.ToTable("Bookings")`
- PK on `Id`
- `TenantId` required, index on `TenantId`
- `ClientId` required, FK to `Clients` with `DeleteBehavior.Restrict`
- `StaffMemberId` required, FK to `StaffMembers` with `DeleteBehavior.Restrict`
- `StartTime` required
- `EndTime` required
- `Notes` optional, max 1000
- `CreatedBy` required
- All other `*By` fields optional
- Index on `(TenantId, StaffMemberId, StartTime)` for overlap queries
- `HasMany(b => b.BookingServices).WithOne(bs => bs.Booking).HasForeignKey(bs => bs.BookingId).OnDelete(DeleteBehavior.Cascade)`

**BookingServiceConfiguration** (`Chairly.Infrastructure/Persistence/Configurations/BookingServiceConfiguration.cs`):
- `builder.ToTable("BookingServices")`
- PK on `Id`
- `BookingId` required
- `ServiceId` required but NO FK constraint (preserves history)
- `ServiceName` required, max 200
- `Price` with `HasPrecision(10, 2)`
- `SortOrder` required

**Migration:** Generate with `dotnet ef migrations add AddBooking --project src/backend/Chairly.Infrastructure --startup-project src/backend/Chairly.Api`

**Test cases:**
- Entity can be saved and retrieved from in-memory DB
- BookingService cascade delete works

### B3 — GetBookingsList query and handler

Create `Chairly.Api/Features/Bookings/GetBookingsList/` slice.

**Route:** `GET /api/bookings`

**Query parameters (all optional):**
- `date` — ISO 8601 date string. Filters `StartTime.Date == date`.
- `staffMemberId` — Guid. Filters by staff member.

**GetBookingsListQuery:**
```csharp
internal sealed record GetBookingsListQuery(DateOnly? Date, Guid? StaffMemberId) : IRequest<IReadOnlyList<BookingResponse>>;
```

**Handler logic:**
1. Query `db.Bookings` for `TenantId == DefaultTenantId`
2. Include `BookingServices` (ordered by `SortOrder`)
3. If `Date` provided, filter `b.StartTime.Date == date`
4. If `StaffMemberId` provided, filter `b.StaffMemberId == staffMemberId`
5. Order by `StartTime` ascending
6. Map to `BookingResponse` (derive status from timestamps)

**BookingResponse** — shared record in `Chairly.Api/Features/Bookings/BookingResponse.cs`:
```csharp
internal sealed record BookingResponse(
    Guid Id,
    Guid ClientId,
    Guid StaffMemberId,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    string? Notes,
    string Status,
    IReadOnlyList<BookingServiceResponse> Services,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc,
    DateTimeOffset? ConfirmedAtUtc,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    DateTimeOffset? CancelledAtUtc,
    DateTimeOffset? NoShowAtUtc);

internal sealed record BookingServiceResponse(
    Guid ServiceId,
    string ServiceName,
    TimeSpan Duration,
    decimal Price,
    int SortOrder);
```

**Endpoint:**
```csharp
group.MapGet("/", async ([AsParameters] GetBookingsListQuery query, IMediator mediator, CancellationToken ct) => ...);
```

Bind `date` and `staffMemberId` as query parameters. Return `200 OK` with `BookingResponse[]`.

**Test cases:**
- Returns empty list when no bookings
- Returns bookings ordered by StartTime
- Filters by date correctly
- Filters by staffMemberId correctly
- Combined filter works

### B4 — GetBooking query and handler

Create `Chairly.Api/Features/Bookings/GetBooking/` slice.

**Route:** `GET /api/bookings/{id}`

**GetBookingQuery:**
```csharp
internal sealed record GetBookingQuery(Guid Id) : IRequest<OneOf<BookingResponse, NotFound>>;
```

**Handler logic:**
1. Find booking by `Id` and `TenantId`, include `BookingServices`
2. If not found, return `NotFound`
3. Map to `BookingResponse`

**Endpoint:** Return `200 OK` with response, or `404 Not Found`.

**Test cases:**
- Happy path returns booking with services
- Not found returns NotFound

### B5 — CreateBooking command and handler

Create `Chairly.Api/Features/Bookings/CreateBooking/` slice.

**Route:** `POST /api/bookings`

**CreateBookingCommand:**
```csharp
internal sealed class CreateBookingCommand : IRequest<OneOf<BookingResponse, NotFound, Conflict>>
{
    [Required] public Guid ClientId { get; set; }
    [Required] public Guid StaffMemberId { get; set; }
    [Required] public DateTimeOffset StartTime { get; set; }
    [Required] [MinLength(1)] public List<Guid> ServiceIds { get; set; } = [];
    [MaxLength(1000)] public string? Notes { get; set; }
}
```

**Handler logic:**
1. Validate `ServiceIds` has at least 1 item (Data Annotations handle this)
2. Look up Client by `ClientId` + `TenantId`, check `DeletedAtUtc == null` -> 404 if not found or soft-deleted
3. Look up StaffMember by `StaffMemberId` + `TenantId`, check `DeactivatedAtUtc == null` -> 404 if not found or deactivated
4. Look up all Services by `ServiceIds` + `TenantId`, check `IsActive == true` for all -> 404 if any missing or inactive
5. Calculate `EndTime = StartTime + sum of service durations`
6. Overlap check: any booking for same staff member where `CancelledAtUtc == null AND NoShowAtUtc == null AND StartTime < newEndTime AND EndTime > newStartTime` -> 409 Conflict
7. Create `Booking` entity with snapshot `BookingService` entries (copy name, duration, price, assign SortOrder)
8. Set `CreatedAtUtc = DateTimeOffset.UtcNow`, `CreatedBy = Guid.Empty`
9. Save and return `201 Created` with `BookingResponse`

**Endpoint:** Return `Results.Created(...)`, `Results.NotFound()`, or `Results.Conflict()`.

**Test cases:**
- Happy path creates booking with snapshotted services
- Returns 404 when client not found
- Returns 404 when client is soft-deleted
- Returns 404 when staff member not found
- Returns 404 when staff member is deactivated
- Returns 404 when any service not found
- Returns 404 when any service is inactive
- Returns 409 when staff overlap detected
- EndTime calculated correctly from service durations
- BookingServices have correct SortOrder

### B6 — UpdateBooking command and handler

Create `Chairly.Api/Features/Bookings/UpdateBooking/` slice.

**Route:** `PUT /api/bookings/{id}`

**UpdateBookingCommand:**
```csharp
internal sealed class UpdateBookingCommand : IRequest<OneOf<BookingResponse, NotFound, Conflict>>
{
    public Guid Id { get; set; }
    [Required] public Guid ClientId { get; set; }
    [Required] public Guid StaffMemberId { get; set; }
    [Required] public DateTimeOffset StartTime { get; set; }
    [Required] [MinLength(1)] public List<Guid> ServiceIds { get; set; } = [];
    [MaxLength(1000)] public string? Notes { get; set; }
}
```

**Handler logic:**
1. Find booking by `Id` + `TenantId`, include `BookingServices` -> 404 if not found
2. Check not in terminal state (CompletedAtUtc, CancelledAtUtc, NoShowAtUtc all null) -> 409 if terminal
3. Same validation as create (client exists, staff exists/active, services exist/active)
4. Recalculate `EndTime`
5. Overlap check **excluding self** (add `&& b.Id != command.Id`) -> 409 if overlap
6. Replace `BookingServices` collection (remove old, add new snapshots)
7. Set `UpdatedAtUtc = DateTimeOffset.UtcNow`, `UpdatedBy = Guid.Empty`
8. Return `200 OK` with `BookingResponse`

**Test cases:**
- Happy path updates booking fields and services
- Returns 404 when booking not found
- Returns 409 when booking is in terminal state
- Returns 409 when update causes overlap
- Overlap check excludes self (updating same time slot is fine)
- UpdatedAtUtc is set

### B7 — Booking action endpoints (cancel, confirm, start, complete, no-show)

Create five slices in `Chairly.Api/Features/Bookings/`:
- `CancelBooking/`
- `ConfirmBooking/`
- `StartBooking/`
- `CompleteBooking/`
- `MarkBookingNoShow/`

All follow the same pattern:

**Command:** `internal sealed record {Action}BookingCommand(Guid Id) : IRequest<OneOf<Success, NotFound, Conflict>>;`

**Handler logic per action:**

| Action | Route | Sets timestamp | Allowed from states | Returns |
|---|---|---|---|---|
| Cancel | `POST /api/bookings/{id}/cancel` | `CancelledAtUtc`, `CancelledBy` | Scheduled, Confirmed, InProgress | 204 / 404 / 409 |
| Confirm | `POST /api/bookings/{id}/confirm` | `ConfirmedAtUtc`, `ConfirmedBy` | Scheduled only | 204 / 404 / 409 |
| Start | `POST /api/bookings/{id}/start` | `StartedAtUtc`, `StartedBy` | Scheduled, Confirmed | 204 / 404 / 409 |
| Complete | `POST /api/bookings/{id}/complete` | `CompletedAtUtc`, `CompletedBy` | InProgress only | 204 / 404 / 409 |
| NoShow | `POST /api/bookings/{id}/no-show` | `NoShowAtUtc`, `NoShowBy` | Scheduled, Confirmed | 204 / 404 / 409 |

Each handler:
1. Find booking by `Id` + `TenantId` -> 404
2. Derive current status from timestamps
3. Check status is in allowed states -> 409 Conflict if not
4. Set the appropriate timestamp pair to `DateTimeOffset.UtcNow` / `Guid.Empty`
5. Save and return `Success`

**Endpoints:** All return `Results.NoContent()`, `Results.NotFound()`, or `Results.Conflict()`.

**BookingEndpoints** (`Chairly.Api/Features/Bookings/BookingEndpoints.cs`):
- Map all endpoints on group `/api/bookings`
- Register in `Program.cs`

**Test cases (per action):**
- Happy path sets timestamp and returns Success
- Returns 404 when booking not found
- Returns 409 when in wrong state
- Specific: Cancel from InProgress is allowed
- Specific: Confirm from Confirmed returns 409
- Specific: Complete from Scheduled returns 409

### B8 — Backend unit tests for booking handlers

Create `Chairly.Tests/Features/Bookings/BookingHandlerTests.cs`.

Follow the pattern from `ServiceHandlerTests.cs`:
- `CreateDbContext()` helper with in-memory DB
- Helper methods to create test Client, StaffMember, Service, and Booking entities
- Test all handlers from B3-B7

**Minimum test coverage:**
- CreateBooking: happy path, client not found, client soft-deleted, staff not found, staff deactivated, service not found, service inactive, overlap detected, EndTime calculation, SortOrder assignment
- UpdateBooking: happy path, not found, terminal state conflict, overlap conflict, self-overlap excluded
- GetBookingsList: empty list, ordered by StartTime, date filter, staff filter
- GetBooking: happy path, not found
- CancelBooking: from Scheduled, from Confirmed, from InProgress, from Completed (conflict), not found
- ConfirmBooking: from Scheduled, from Confirmed (conflict), not found
- StartBooking: from Scheduled, from Confirmed, from InProgress (conflict), not found
- CompleteBooking: from InProgress, from Scheduled (conflict), not found
- MarkBookingNoShow: from Scheduled, from Confirmed, from InProgress (conflict), not found

## Frontend Tasks

### F1 — Booking models

**Folder:** `libs/chairly/src/lib/bookings/models/`

Create TypeScript interfaces in `booking.models.ts`:

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

Export everything from `models/index.ts`. Delete `models/.gitkeep` if it exists (no `.gitkeep` once real files are present).

### F2 — BookingApiService

**Folder:** `libs/chairly/src/lib/bookings/data-access/`

Create `booking-api.service.ts` following the pattern of `ServiceApiService`:

```typescript
@Injectable({ providedIn: 'root' })
export class BookingApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  getBookings(filter?: BookingFilter): Observable<Booking[]>
  getBooking(id: string): Observable<Booking>
  createBooking(request: CreateBookingRequest): Observable<Booking>
  updateBooking(id: string, request: UpdateBookingRequest): Observable<Booking>
  cancelBooking(id: string): Observable<void>
  confirmBooking(id: string): Observable<void>
  startBooking(id: string): Observable<void>
  completeBooking(id: string): Observable<void>
  markNoShow(id: string): Observable<void>
}
```

- Base path: `${this.baseUrl}/bookings`
- `getBookings`: use `HttpParams` to pass `date` and `staffMemberId` when present
- Action endpoints (`cancel`, `confirm`, `start`, `complete`, `no-show`): `POST` with empty body (`null`)
- Import `API_BASE_URL` from `@org/shared-lib`

Delete `data-access/.gitkeep` if it exists.

### F3 — BookingStore

**Folder:** `libs/chairly/src/lib/bookings/data-access/`

Create `booking.store.ts` following the pattern of `ServiceStore`:

**State:**
```typescript
export interface BookingState {
  bookings: Booking[];
  selectedBooking: Booking | null;
  loading: boolean;
  error: string | null;
  activeFilter: BookingFilter;
}
```

**Methods:**
- `loadBookings(filter?: BookingFilter)` — sets `activeFilter` and `loading: true`, calls `getBookings`, updates `bookings`
- `createBooking(request: CreateBookingRequest)` — calls API, then `loadBookings(activeFilter)` to refresh
- `updateBooking(id, request: UpdateBookingRequest)` — calls API, then `loadBookings(activeFilter)`
- `cancelBooking(id)` — calls API, then `loadBookings(activeFilter)`
- `confirmBooking(id)` — calls API, then `loadBookings(activeFilter)`
- `startBooking(id)` — calls API, then `loadBookings(activeFilter)`
- `completeBooking(id)` — calls API, then `loadBookings(activeFilter)`
- `markNoShow(id)` — calls API, then `loadBookings(activeFilter)`
- `selectBooking(booking: Booking | null)` — sets `selectedBooking`

Use `take(1)` on all subscriptions. Use `toErrorMessage` helper like `ServiceStore`.

Export store and service from `data-access/index.ts`.

### F4 — Booking list page (smart component)

**Folder:** `libs/chairly/src/lib/bookings/feature/booking-list-page/`

Smart container component `BookingListPageComponent` (`selector: 'chairly-booking-list-page'`).

- Standalone, `OnPush` change detection
- `templateUrl: './booking-list-page.component.html'`
- Inject `BookingStore`
- On init: call `store.loadBookings()`

**Template:**
- Page heading: `<h1>Boekingen</h1>`
- Filter row:
  - Date input (`type="date"`, label "Datum")
  - Staff member ID input (`type="text"`, placeholder "Medewerker ID")
  - "Filteren" button that calls `store.loadBookings(filter)`
- "Nieuwe boeking" button (primary) that opens the create dialog
- `<chairly-booking-table [bookings]="store.bookings()" (bookingSelected)="..." (statusAction)="...">`
- `<chairly-booking-form-dialog>` controlled by `dialogOpen` and `editingBooking` signals

**Signals:**
- `dialogOpen = signal(false)`
- `editingBooking = signal<Booking | null>(null)`

**Event handlers:**
- `bookingSelected` -> set `editingBooking`, set `dialogOpen = true`
- `statusAction` with `{ action, bookingId }` -> call appropriate store method (`confirmBooking`, `startBooking`, etc.)
- Dialog `saved` event -> call `store.createBooking()` or `store.updateBooking()` depending on whether `editingBooking` is set
- Dialog `cancelled` event -> set `dialogOpen = false`, set `editingBooking = null`

Export from `feature/index.ts`. Delete `feature/.gitkeep`.

### F5 — Booking table component (presentational)

**Folder:** `libs/chairly/src/lib/bookings/ui/`

Presentational component `BookingTableComponent` (`selector: 'chairly-booking-table'`).

**Inputs (`input()`):**
- `bookings: Booking[]`

**Outputs (`OutputEmitterRef`):**
- `bookingSelected` emits `Booking`
- `statusAction` emits `{ action: 'confirm' | 'start' | 'complete' | 'cancel' | 'noShow', bookingId: string }`

**Template columns (Dutch headers):**

| Kolomkop | Inhoud |
|---|---|
| Datum & tijd | `startTime` formatted `dd-MM-yyyy HH:mm` — `endTime` formatted `HH:mm` |
| Klant | `clientId` (placeholder, client lookup out of scope) |
| Medewerker | `staffMemberId` (placeholder) |
| Diensten | comma-separated `serviceName` values |
| Status | badge with Dutch label and color |
| Acties | `<chairly-booking-status-actions>` |

**Status badge mapping:**

| Status | Dutch label | Color |
|---|---|---|
| Scheduled | Gepland | gray |
| Confirmed | Bevestigd | blue |
| InProgress | Bezig | yellow/amber |
| Completed | Voltooid | green |
| Cancelled | Geannuleerd | red |
| NoShow | Niet-verschenen | orange |

Clicking a row emits `bookingSelected` with that booking.

Use a pipe (`BookingStatusPipe`) in `libs/chairly/src/lib/bookings/pipes/` to translate status to Dutch label. Create `pipes/index.ts` barrel.

### F6 — Booking form dialog (presentational)

**Folder:** `libs/chairly/src/lib/bookings/ui/`

Presentational component `BookingFormDialogComponent` (`selector: 'chairly-booking-form-dialog'`).

**Inputs:**
- `open: boolean` — controls visibility via `showModal()` / `close()`
- `booking: Booking | null` — when set, pre-fills form (edit mode)

**Outputs:**
- `saved` emits `{ id: string | null, request: CreateBookingRequest }` — `id` is null for create
- `cancelled` emits void

**Form fields (reactive form with typed FormGroup):**

| Veld | Type | Validatie |
|---|---|---|
| Klant ID | text | required |
| Medewerker ID | text | required |
| Datum & tijd | datetime-local | required |
| Dienst-IDs | text (comma-separated) | required, at least one value |
| Notities | textarea | optional, max 1000 chars |

**Dialog title:**
- Create: "Nieuwe boeking"
- Edit: "Boeking bewerken"

**Buttons:** "Opslaan" (primary, submits form) / "Annuleren" (secondary, emits `cancelled`)

Follow the native `<dialog>` pattern from CLAUDE.md:
- Use `showModal()` / `close()`
- Full-screen overlay with centered card
- Inject `DOCUMENT`, set `document.body.style.overflow` on open/close
- `[open]` CSS attribute selector to prevent pointer event capture when closed

### F7 — Booking status actions component (presentational)

**Folder:** `libs/chairly/src/lib/bookings/ui/`

Presentational component `BookingStatusActionsComponent` (`selector: 'chairly-booking-status-actions'`).

**Inputs:**
- `booking: Booking`

**Outputs:**
- `action` emits `{ action: 'confirm' | 'start' | 'complete' | 'cancel' | 'noShow', bookingId: string }`

**Visible buttons by status:**

| Status | Buttons (Dutch) |
|---|---|
| Scheduled | Bevestigen, Starten, Annuleren, Niet-verschenen |
| Confirmed | Starten, Annuleren, Niet-verschenen |
| InProgress | Voltooien, Annuleren |
| Completed | _(none)_ |
| Cancelled | _(none)_ |
| NoShow | _(none)_ |

Each button emits `action` with its key and `booking.id`. Use small Tailwind-styled buttons with appropriate colors.

Export all UI components from `ui/index.ts`. Delete `ui/.gitkeep`.

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

Add a "Boekingen" navigation link in the app shell sidebar/nav.

Export `bookingsRoutes` from the chairly-lib barrel (`libs/chairly/src/lib/index.ts` or `libs/chairly/src/index.ts`).

### F9 — Playwright e2e tests

**File:** `apps/chairly-e2e/src/bookings.spec.ts`

Follow the pattern from `services.spec.ts` (mock API routes, navigate, assert):

**Mock data:**
- `mockBooking` with status "Scheduled" (only `createdAtUtc` set)
- Mock API routes for `GET /api/bookings`, `POST /api/bookings`, `POST /api/bookings/{id}/confirm`, etc.

**Scenarios:**

1. **Lijst laadt** — navigate to `/bookings`, verify heading "Boekingen" and table visible, mock booking row appears
2. **Nieuwe boeking aanmaken** — click "Nieuwe boeking", fill in fields (clientId, staffMemberId, datetime, serviceIds), click "Opslaan", verify POST was called and row appears
3. **Status actie: Bevestigen** — find Scheduled booking, click "Bevestigen", verify API called and status badge changes to "Bevestigd"
4. **Boeking bewerken** — click a row, verify dialog opens in edit mode with pre-filled values, change notes, click "Opslaan"

Use `page.route()` for API mocking. Use `page.keyboard.press('Escape')` to close dialogs (per CLAUDE.md convention for cross-browser Playwright reliability).

## Acceptance Criteria

**Backend:**
- [ ] GET /api/bookings returns bookings filtered by date and/or staffMemberId
- [ ] GET /api/bookings/{id} returns a single booking with services
- [ ] POST /api/bookings creates a booking with validation (client, staff, services, overlap)
- [ ] PUT /api/bookings/{id} updates a booking; blocked in terminal states
- [ ] POST /api/bookings/{id}/cancel sets CancelledAtUtc; allowed from Scheduled, Confirmed, InProgress
- [ ] POST /api/bookings/{id}/confirm sets ConfirmedAtUtc; only from Scheduled
- [ ] POST /api/bookings/{id}/start sets StartedAtUtc; from Scheduled or Confirmed
- [ ] POST /api/bookings/{id}/complete sets CompletedAtUtc; only from InProgress
- [ ] POST /api/bookings/{id}/no-show sets NoShowAtUtc; from Scheduled or Confirmed
- [ ] EndTime = StartTime + sum of service durations
- [ ] Service data snapshotted at creation time
- [ ] Overlap detection excludes cancelled/no-show bookings
- [ ] Unit tests pass for all handlers

**Frontend:**
- [ ] Booking list page renders at /bookings with heading "Boekingen"
- [ ] Table displays bookings with Dutch status badges
- [ ] Date and staff member filters work
- [ ] Create dialog opens, submits, and refreshes list
- [ ] Edit dialog opens pre-filled, submits, and refreshes list
- [ ] Status action buttons visible per state, trigger correct API calls
- [ ] All UI text in Dutch
- [ ] Playwright e2e tests pass

**Quality gates:**
- [ ] `dotnet build src/backend/Chairly.slnx` passes
- [ ] `dotnet test src/backend/Chairly.slnx` passes
- [ ] `dotnet format src/backend/Chairly.slnx --verify-no-changes` passes
- [ ] `npx nx affected -t lint --base=main` passes
- [ ] `npx nx format:check --base=main` passes
- [ ] `npx nx affected -t test --base=main` passes
- [ ] `npx nx affected -t build --base=main` passes
- [ ] Playwright e2e tests pass

## Out of Scope

- Recurring bookings
- Multi-staff bookings (one booking = one staff member)
- Client self-service / online booking
- Working hours validation (staff availability check against WorkingHoursEntry)
- Payment / invoice creation on completion (Billing context, separate feature)
- Notifications (BookingCreated, BookingCancelled events — future feature)
- Client name lookup in frontend table (placeholder with clientId for now)
- Staff member name lookup in frontend table (placeholder with staffMemberId for now)
