# Bookings Backend

> **Status: Implemented** — Merged to main.

## Overview

Bookings are the core of Chairly. A booking represents a scheduled visit by a client with a staff member, containing one or more services. This spec covers the full backend API for the Bookings bounded context: CRUD operations, state transitions (confirm, start, complete, cancel, no-show), overlap detection, and service snapshotting. The frontend will be a separate feature.

## Domain Context

- Bounded context: **Bookings**
- Key entities involved: **Booking** (aggregate root), **BookingService** (owned entity), **Client**, **StaffMember**, **Service**
- Ubiquitous language: Booking (never "appointment"), Client (never "customer"), Staff Member (never "employee"), Service, No-Show, Terminal state

## Backend Tasks

### B1 — Booking and BookingService domain entities

Create the `Booking` and `BookingService` domain entities in `Chairly.Domain/Entities/`.

**Booking entity** (`Chairly.Domain/Entities/Booking.cs`):

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | PK |
| `TenantId` | `Guid` | FK (cross-cutting) |
| `ClientId` | `Guid` | FK to Clients |
| `StaffMemberId` | `Guid` | FK to StaffMembers |
| `StartTime` | `DateTimeOffset` | required |
| `EndTime` | `DateTimeOffset` | calculated: StartTime + sum of service durations |
| `Notes` | `string?` | optional, max 1000 |
| `CreatedAtUtc` | `DateTimeOffset` | required |
| `CreatedBy` | `Guid` | required (Guid.Empty until auth) |
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

Include navigation properties for `Client` and `StaffMember` (for EF Core loading).

**BookingService entity** (`Chairly.Domain/Entities/BookingService.cs`):

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | PK (auto-generated, EF key only) |
| `BookingId` | `Guid` | FK to Booking |
| `ServiceId` | `Guid` | reference only (NO FK constraint — preserves history if service deleted) |
| `ServiceName` | `string` | snapshot of Service.Name, max 200 |
| `Duration` | `TimeSpan` | snapshot of Service.Duration |
| `Price` | `decimal` | snapshot of Service.Price |
| `SortOrder` | `int` | order within the booking |

Include a navigation property back to `Booking`.

**Test cases:**
- No tests for this task (entities are plain POCOs)

---

### B2 — EF Core configuration and DbContext registration

Create EF Core configurations and register the entities in `ChairlyDbContext`.

**BookingConfiguration** (`Chairly.Infrastructure/Persistence/Configurations/BookingConfiguration.cs`):
- Table name: `"Bookings"`
- PK: `Id`
- `TenantId`: required, indexed
- `ClientId`: required, FK to `Clients` table, `DeleteBehavior.Restrict`
- `StaffMemberId`: required, FK to `StaffMembers` table, `DeleteBehavior.Restrict`
- `StartTime`: required
- `EndTime`: required
- `Notes`: optional, max 1000
- `CreatedBy`: required
- All other `*By` fields: optional
- Navigation: `HasMany(b => b.BookingServices).WithOne(bs => bs.Booking).HasForeignKey(bs => bs.BookingId).OnDelete(DeleteBehavior.Cascade)`
- Composite index on `(TenantId, StaffMemberId, StartTime)` for overlap query performance
- Index on `(TenantId, StartTime)` for date-filtered list queries

**BookingServiceConfiguration** (`Chairly.Infrastructure/Persistence/Configurations/BookingServiceConfiguration.cs`):
- Table name: `"BookingServices"`
- PK: `Id`
- `BookingId`: required (FK handled by Booking config)
- `ServiceId`: required, NO FK constraint (intentionally — preserves booking history if service is later deleted)
- `ServiceName`: required, max 200
- `Price`: precision(10, 2)
- `SortOrder`: required

**DbContext changes** (`Chairly.Infrastructure/Persistence/ChairlyDbContext.cs`):
- Add `DbSet<Booking> Bookings => Set<Booking>();`
- Add `DbSet<BookingService> BookingServices => Set<BookingService>();`

**EF Migration:**
- Create migration after configurations are in place: `dotnet ef migrations add AddBookingEntities`
- The migration should create the `Bookings` and `BookingServices` tables with appropriate indexes and FK constraints

**Test cases:**
- No dedicated tests (configuration correctness is validated by integration tests in later tasks)

---

### B3 — BookingResponse and BookingServiceResponse records

Create shared response records used by all booking endpoints.

**BookingServiceResponse** (in `Chairly.Api/Features/Bookings/BookingServiceResponse.cs`):
```csharp
internal sealed record BookingServiceResponse(
    Guid ServiceId,
    string ServiceName,
    TimeSpan Duration,
    decimal Price,
    int SortOrder);
```

**BookingResponse** (in `Chairly.Api/Features/Bookings/BookingResponse.cs`):
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
```

**Status derivation helper** — Create a static method `DeriveStatus(Booking)` returning a string, either as a static helper on a shared class or in a utility file within the Bookings feature folder. The method must follow this priority order:
1. `CancelledAtUtc != null` -> `"Cancelled"`
2. `NoShowAtUtc != null` -> `"NoShow"`
3. `CompletedAtUtc != null` -> `"Completed"`
4. `StartedAtUtc != null` -> `"InProgress"`
5. `ConfirmedAtUtc != null` -> `"Confirmed"`
6. else -> `"Scheduled"`

**ToResponse mapper** — Create a static `ToResponse(Booking)` method that maps a `Booking` (with loaded `BookingServices`) to a `BookingResponse`. This will be reused across Create, Update, Get, and List handlers.

**Test cases:**
- Unit test: `DeriveStatus` returns correct status for each combination of timestamps (6 cases)
- Unit test: `DeriveStatus` priority — when both `CancelledAtUtc` and `CompletedAtUtc` are set, returns `"Cancelled"` (terminal state precedence)

---

### B4 — Create Booking endpoint

Create the `POST /api/bookings` endpoint following the vertical slice pattern.

**Slice location:** `Chairly.Api/Features/Bookings/CreateBooking/`

**CreateBookingCommand** (`CreateBookingCommand.cs`):
```csharp
internal sealed class CreateBookingCommand : IRequest<OneOf<BookingResponse, NotFound, Conflict>>
{
    [Required]
    public Guid ClientId { get; set; }

    [Required]
    public Guid StaffMemberId { get; set; }

    [Required]
    public DateTimeOffset StartTime { get; set; }

    [Required]
    [MinLength(1)]
    public List<Guid> ServiceIds { get; set; } = [];

    [MaxLength(1000)]
    public string? Notes { get; set; }
}
```

**CreateBookingHandler** (`CreateBookingHandler.cs`):

Handler logic (in order):
1. Validate `ServiceIds` has at least 1 item (Data Annotations covers this via `[MinLength(1)]`, but the ValidationBehavior will catch it)
2. Query Client: must exist for tenant and `DeletedAtUtc == null` -> return `NotFound` if not found
3. Query StaffMember: must exist for tenant and `DeactivatedAtUtc == null` -> return `NotFound` if not found
4. Query Services: all `ServiceIds` must exist for tenant and `IsActive == true` -> return `NotFound` if any are missing
5. Calculate `EndTime = StartTime + sum of all service durations`
6. Overlap check: query for any booking for the same `StaffMemberId` where `CancelledAtUtc == null AND NoShowAtUtc == null AND StartTime < newEndTime AND EndTime > newStartTime` -> return `Conflict` if overlap found
7. Create `Booking` entity with `Id = Guid.NewGuid()`, `TenantId = TenantConstants.DefaultTenantId`, `CreatedAtUtc = DateTimeOffset.UtcNow`, `CreatedBy = Guid.Empty`
8. Create `BookingService` entries: snapshot `ServiceName`, `Duration`, `Price` from each `Service`, assign `SortOrder` based on position in `ServiceIds` list
9. Save to database
10. Return `BookingResponse`

**CreateBookingEndpoint** (`CreateBookingEndpoint.cs`):
- `POST /` on the bookings group
- Returns `Results.Created($"/api/bookings/{result.Id}", result)` on success
- Returns `Results.NotFound()` for not found
- Returns `Results.Conflict()` for overlap

**Test cases:**
- Happy path: creates booking with correct fields, returns 201 with BookingResponse
- Snapshots service data (name, duration, price) correctly
- Calculates EndTime correctly (StartTime + sum of durations)
- Returns NotFound when client does not exist
- Returns NotFound when client is soft-deleted
- Returns NotFound when staff member does not exist
- Returns NotFound when staff member is deactivated
- Returns NotFound when one or more services do not exist
- Returns NotFound when a service is inactive
- Returns Conflict when there is an overlapping booking for the staff member
- Does not conflict with cancelled bookings (overlap check excludes cancelled)
- Does not conflict with no-show bookings (overlap check excludes no-show)
- Multiple services are saved with correct sort order

---

### B5 — Get Booking endpoint

Create the `GET /api/bookings/{id}` endpoint.

**Slice location:** `Chairly.Api/Features/Bookings/GetBooking/`

**GetBookingQuery** (`GetBookingQuery.cs`):
```csharp
internal sealed record GetBookingQuery(Guid Id) : IRequest<OneOf<BookingResponse, NotFound>>;
```

**GetBookingHandler** (`GetBookingHandler.cs`):
- Query booking by `Id` and `TenantId`, include `BookingServices`
- Return `NotFound` if not found
- Return `BookingResponse` with derived status

**GetBookingEndpoint** (`GetBookingEndpoint.cs`):
- `GET /{id:guid}` on the bookings group
- Returns `Results.Ok(response)` or `Results.NotFound()`

**Test cases:**
- Happy path: returns booking with services and correct derived status
- Returns NotFound for non-existent booking
- Returns NotFound for booking belonging to different tenant (when tenant scoping is active)
- BookingServices are returned in SortOrder

---

### B6 — Get Bookings List endpoint

Create the `GET /api/bookings` endpoint with optional filters.

**Slice location:** `Chairly.Api/Features/Bookings/GetBookingsList/`

**GetBookingsListQuery** (`GetBookingsListQuery.cs`):
```csharp
internal sealed record GetBookingsListQuery(DateOnly? Date, Guid? StaffMemberId) : IRequest<IEnumerable<BookingResponse>>;
```

**GetBookingsListHandler** (`GetBookingsListHandler.cs`):
- Query bookings for `TenantId`, include `BookingServices`
- If `Date` is provided, filter where `StartTime.Date == date` (compare date portion only)
- If `StaffMemberId` is provided, filter by staff member
- Order by `StartTime` ascending
- Map each to `BookingResponse` with derived status

**GetBookingsListEndpoint** (`GetBookingsListEndpoint.cs`):
- `GET /` on the bookings group
- Bind query parameters: `date` (DateOnly?, optional), `staffMemberId` (Guid?, optional)
- Returns `Results.Ok(result)`

**Test cases:**
- Happy path: returns all bookings ordered by StartTime
- Filters by date correctly
- Filters by staffMemberId correctly
- Filters by both date and staffMemberId together
- Returns empty array when no bookings match
- Each booking includes its services with correct derived status

---

### B7 — Update Booking endpoint

Create the `PUT /api/bookings/{id}` endpoint.

**Slice location:** `Chairly.Api/Features/Bookings/UpdateBooking/`

**UpdateBookingCommand** (`UpdateBookingCommand.cs`):
```csharp
internal sealed class UpdateBookingCommand : IRequest<OneOf<BookingResponse, NotFound, Conflict>>
{
    public Guid Id { get; set; }

    [Required]
    public Guid ClientId { get; set; }

    [Required]
    public Guid StaffMemberId { get; set; }

    [Required]
    public DateTimeOffset StartTime { get; set; }

    [Required]
    [MinLength(1)]
    public List<Guid> ServiceIds { get; set; } = [];

    [MaxLength(1000)]
    public string? Notes { get; set; }
}
```

**UpdateBookingHandler** (`UpdateBookingHandler.cs`):

Handler logic:
1. Find booking by `Id` and `TenantId`, include `BookingServices` -> return `NotFound` if not found
2. Check terminal state: if `CompletedAtUtc`, `CancelledAtUtc`, or `NoShowAtUtc` is set -> return `Conflict`
3. Validate Client exists and is not soft-deleted -> return `NotFound`
4. Validate StaffMember exists and is not deactivated -> return `NotFound`
5. Validate all ServiceIds exist and are active -> return `NotFound`
6. Calculate new `EndTime`
7. Overlap check (exclude the booking being updated — `b.Id != command.Id`) -> return `Conflict`
8. Update booking fields: `ClientId`, `StaffMemberId`, `StartTime`, `EndTime`, `Notes`
9. Replace `BookingServices`: remove existing, add new snapshots
10. Set `UpdatedAtUtc = DateTimeOffset.UtcNow`, `UpdatedBy = Guid.Empty`
11. Save and return `BookingResponse`

**UpdateBookingEndpoint** (`UpdateBookingEndpoint.cs`):
- `PUT /{id:guid}` on the bookings group
- Set `command.Id = id` from route
- Returns `Results.Ok(response)`, `Results.NotFound()`, or `Results.Conflict()`

**Test cases:**
- Happy path: updates booking and returns updated response
- Returns NotFound when booking does not exist
- Returns Conflict when booking is in terminal state (Completed)
- Returns Conflict when booking is in terminal state (Cancelled)
- Returns Conflict when booking is in terminal state (NoShow)
- Returns NotFound when new client does not exist
- Returns NotFound when new staff member is deactivated
- Returns NotFound when a new service is inactive
- Overlap check excludes the booking itself (updating time of same booking does not self-conflict)
- Returns Conflict when new time overlaps with another booking
- BookingServices are replaced (old removed, new added with fresh snapshots)
- Sets UpdatedAtUtc and UpdatedBy

---

### B8 — Cancel Booking endpoint

Create the `POST /api/bookings/{id}/cancel` endpoint.

**Slice location:** `Chairly.Api/Features/Bookings/CancelBooking/`

**CancelBookingCommand** (`CancelBookingCommand.cs`):
```csharp
internal sealed record CancelBookingCommand(Guid Id) : IRequest<OneOf<Success, NotFound, Conflict>>;
```

**CancelBookingHandler** (`CancelBookingHandler.cs`):
- Find booking by `Id` and `TenantId` -> return `NotFound`
- Check if already in terminal state (`CompletedAtUtc`, `CancelledAtUtc`, or `NoShowAtUtc` is set) -> return `Conflict`
- Set `CancelledAtUtc = DateTimeOffset.UtcNow`, `CancelledBy = Guid.Empty`
- Save and return `Success`

Note: Cancelling an InProgress booking IS allowed (client leaves mid-service).

**CancelBookingEndpoint** (`CancelBookingEndpoint.cs`):
- `POST /{id:guid}/cancel`
- Returns `Results.NoContent()`, `Results.NotFound()`, or `Results.Conflict()`

**Test cases:**
- Happy path from Scheduled state: sets CancelledAtUtc, returns 204
- Happy path from Confirmed state: sets CancelledAtUtc
- Happy path from InProgress state: sets CancelledAtUtc (allowed)
- Returns NotFound for non-existent booking
- Returns Conflict when already Completed
- Returns Conflict when already Cancelled
- Returns Conflict when already NoShow

---

### B9 — Confirm Booking endpoint

Create the `POST /api/bookings/{id}/confirm` endpoint.

**Slice location:** `Chairly.Api/Features/Bookings/ConfirmBooking/`

**ConfirmBookingCommand** (`ConfirmBookingCommand.cs`):
```csharp
internal sealed record ConfirmBookingCommand(Guid Id) : IRequest<OneOf<Success, NotFound, Conflict>>;
```

**ConfirmBookingHandler** (`ConfirmBookingHandler.cs`):
- Find booking by `Id` and `TenantId` -> return `NotFound`
- Must be in Scheduled state: `ConfirmedAtUtc == null && StartedAtUtc == null && CompletedAtUtc == null && CancelledAtUtc == null && NoShowAtUtc == null` -> return `Conflict` otherwise
- Set `ConfirmedAtUtc = DateTimeOffset.UtcNow`, `ConfirmedBy = Guid.Empty`
- Save and return `Success`

**ConfirmBookingEndpoint** (`ConfirmBookingEndpoint.cs`):
- `POST /{id:guid}/confirm`
- Returns `Results.NoContent()`, `Results.NotFound()`, or `Results.Conflict()`

**Test cases:**
- Happy path from Scheduled: sets ConfirmedAtUtc, returns 204
- Returns Conflict when already Confirmed
- Returns Conflict when InProgress
- Returns Conflict when Completed
- Returns Conflict when Cancelled
- Returns Conflict when NoShow
- Returns NotFound for non-existent booking

---

### B10 — Start Booking endpoint

Create the `POST /api/bookings/{id}/start` endpoint.

**Slice location:** `Chairly.Api/Features/Bookings/StartBooking/`

**StartBookingCommand** (`StartBookingCommand.cs`):
```csharp
internal sealed record StartBookingCommand(Guid Id) : IRequest<OneOf<Success, NotFound, Conflict>>;
```

**StartBookingHandler** (`StartBookingHandler.cs`):
- Find booking by `Id` and `TenantId` -> return `NotFound`
- Must be in Scheduled or Confirmed state: `StartedAtUtc == null && CompletedAtUtc == null && CancelledAtUtc == null && NoShowAtUtc == null` -> return `Conflict` otherwise
- Set `StartedAtUtc = DateTimeOffset.UtcNow`, `StartedBy = Guid.Empty`
- Save and return `Success`

**StartBookingEndpoint** (`StartBookingEndpoint.cs`):
- `POST /{id:guid}/start`
- Returns `Results.NoContent()`, `Results.NotFound()`, or `Results.Conflict()`

**Test cases:**
- Happy path from Scheduled: sets StartedAtUtc, returns 204
- Happy path from Confirmed: sets StartedAtUtc (skip confirm is allowed)
- Returns Conflict when already InProgress
- Returns Conflict when Completed
- Returns Conflict when Cancelled
- Returns Conflict when NoShow
- Returns NotFound for non-existent booking

---

### B11 — Complete Booking endpoint

Create the `POST /api/bookings/{id}/complete` endpoint.

**Slice location:** `Chairly.Api/Features/Bookings/CompleteBooking/`

**CompleteBookingCommand** (`CompleteBookingCommand.cs`):
```csharp
internal sealed record CompleteBookingCommand(Guid Id) : IRequest<OneOf<Success, NotFound, Conflict>>;
```

**CompleteBookingHandler** (`CompleteBookingHandler.cs`):
- Find booking by `Id` and `TenantId` -> return `NotFound`
- Must be InProgress: `StartedAtUtc != null && CompletedAtUtc == null && CancelledAtUtc == null && NoShowAtUtc == null` -> return `Conflict` otherwise
- Set `CompletedAtUtc = DateTimeOffset.UtcNow`, `CompletedBy = Guid.Empty`
- Save and return `Success`

**CompleteBookingEndpoint** (`CompleteBookingEndpoint.cs`):
- `POST /{id:guid}/complete`
- Returns `Results.NoContent()`, `Results.NotFound()`, or `Results.Conflict()`

**Test cases:**
- Happy path from InProgress: sets CompletedAtUtc, returns 204
- Returns Conflict when Scheduled (not yet started)
- Returns Conflict when Confirmed (not yet started)
- Returns Conflict when already Completed
- Returns Conflict when Cancelled
- Returns Conflict when NoShow
- Returns NotFound for non-existent booking

---

### B12 — No-Show Booking endpoint

Create the `POST /api/bookings/{id}/no-show` endpoint.

**Slice location:** `Chairly.Api/Features/Bookings/NoShowBooking/`

**NoShowBookingCommand** (`NoShowBookingCommand.cs`):
```csharp
internal sealed record NoShowBookingCommand(Guid Id) : IRequest<OneOf<Success, NotFound, Conflict>>;
```

**NoShowBookingHandler** (`NoShowBookingHandler.cs`):
- Find booking by `Id` and `TenantId` -> return `NotFound`
- Must be in Scheduled or Confirmed state: `StartedAtUtc == null && CompletedAtUtc == null && CancelledAtUtc == null && NoShowAtUtc == null` -> return `Conflict` otherwise
- Set `NoShowAtUtc = DateTimeOffset.UtcNow`, `NoShowBy = Guid.Empty`
- Save and return `Success`

**NoShowBookingEndpoint** (`NoShowBookingEndpoint.cs`):
- `POST /{id:guid}/no-show`
- Returns `Results.NoContent()`, `Results.NotFound()`, or `Results.Conflict()`

**Test cases:**
- Happy path from Scheduled: sets NoShowAtUtc, returns 204
- Happy path from Confirmed: sets NoShowAtUtc
- Returns Conflict when InProgress
- Returns Conflict when Completed
- Returns Conflict when Cancelled
- Returns Conflict when already NoShow
- Returns NotFound for non-existent booking

---

### B13 — Booking endpoint registration and wiring

Create `BookingEndpoints.cs` to register all booking endpoints and wire them into `Program.cs`.

**BookingEndpoints** (`Chairly.Api/Features/Bookings/BookingEndpoints.cs`):
```csharp
internal static class BookingEndpoints
{
    public static IEndpointRouteBuilder MapBookingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/bookings");

        group.MapGetBookingsList();
        group.MapGetBooking();
        group.MapCreateBooking();
        group.MapUpdateBooking();
        group.MapCancelBooking();
        group.MapConfirmBooking();
        group.MapStartBooking();
        group.MapCompleteBooking();
        group.MapNoShowBooking();

        return app;
    }
}
```

**Program.cs changes:**
- Add `app.MapBookingEndpoints();` alongside existing endpoint registrations

**Test cases:**
- No dedicated tests (covered by integration/endpoint tests)

---

### B14 — Unit tests for booking handlers

Create comprehensive unit tests in `Chairly.Tests/Features/Bookings/BookingHandlerTests.cs`.

Follow the existing test patterns (see `ClientHandlerTests.cs`, `ServiceHandlerTests.cs`):
- Use `InMemoryDatabase` for DbContext
- Create helper methods for test data setup (e.g. `CreateTestBooking`, `CreateTestClient`, `CreateTestStaffMember`, `CreateTestService`)
- Each test method tests one scenario
- Use `[Fact]` attribute

**Test file:** `Chairly.Tests/Features/Bookings/BookingHandlerTests.cs`

Cover all test cases listed in B3 through B12. Key test scenarios:

**Status derivation (B3):**
- All 6 status values derived correctly
- Terminal state precedence (Cancelled > NoShow > Completed > InProgress > Confirmed > Scheduled)

**Create (B4):**
- Happy path with service snapshotting
- EndTime calculation
- All validation failures (client not found, staff inactive, service inactive, overlap)
- Overlap exclusions (cancelled and no-show bookings do not block)

**Get (B5):**
- Happy path, not found

**List (B6):**
- No filter, date filter, staff filter, combined filter, empty result

**Update (B7):**
- Happy path, terminal state blocked, self-overlap allowed, service replacement

**State transitions (B8-B12):**
- Each valid transition succeeds
- Each invalid transition returns Conflict
- Not found returns NotFound

## Acceptance Criteria

- [ ] GET /api/bookings returns all bookings for tenant, filterable by date and staffMemberId
- [ ] GET /api/bookings/{id} returns a single booking with services
- [ ] POST /api/bookings creates a booking, validates client/staff/services, checks overlap, snapshots services
- [ ] PUT /api/bookings/{id} updates a booking; blocked in terminal states
- [ ] POST /api/bookings/{id}/cancel sets CancelledAtUtc; blocked in terminal states
- [ ] POST /api/bookings/{id}/confirm sets ConfirmedAtUtc; only from Scheduled
- [ ] POST /api/bookings/{id}/start sets StartedAtUtc; only from Scheduled or Confirmed
- [ ] POST /api/bookings/{id}/complete sets CompletedAtUtc; only from InProgress
- [ ] POST /api/bookings/{id}/no-show sets NoShowAtUtc; only from Scheduled or Confirmed
- [ ] EndTime = StartTime + sum of service durations
- [ ] Service name, duration, price are snapshotted at booking creation
- [ ] Overlap detection excludes cancelled and no-show bookings
- [ ] Status is derived from timestamps (no status column) per ADR-009
- [ ] All backend quality checks pass: `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`

## Out of Scope

- Recurring bookings
- Multi-staff bookings
- Client self-service / online booking
- Working hours validation (staff availability against WorkingHoursEntry)
- Payment / invoice creation on completion (Billing context)
- Notifications (separate feature)
- Domain events (BookingCreated, BookingCancelled, etc. — future iteration)
- Frontend (separate feature)
