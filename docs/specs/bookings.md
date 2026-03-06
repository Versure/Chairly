# Feature: Bookings Backend

## Context

Bookings are the core of Chairly. A booking represents a scheduled visit by a client with a staff member, containing one or more services. This spec covers the backend API only. The frontend will be a separate feature.

## User Stories

- As an owner or manager, I want to view all bookings so I can see the salon's schedule.
- As a staff member, I want to view my own bookings so I can see my upcoming appointments.
- As an owner, manager, or staff member, I want to create a booking so I can schedule a client visit.
- As an owner or manager, I want to update a booking so I can correct mistakes or reschedule.
- As an owner, manager, or staff member, I want to cancel a booking so I can handle cancellations.
- As an owner or manager, I want to confirm, start, complete, or mark a booking as no-show to track its progress through the salon.

## Acceptance Criteria

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

## Out of Scope

- Recurring bookings
- Multi-staff bookings
- Client self-service / online booking
- Working hours validation (staff availability check against WorkingHoursEntry)
- Payment / invoice creation on completion (Billing context, separate feature)
- Notifications (separate feature)
