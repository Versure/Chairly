# ADR-009: Timestamps Instead of Status Columns

## Status

Accepted

## Context

Entities in the domain model go through lifecycle states (e.g. a booking goes from scheduled → confirmed → completed). The traditional approach uses a `Status` enum column. An alternative is to use nullable timestamp fields that record **when** and **by whom** each state transition occurred.

## Decision

We use **nullable timestamp fields** instead of status enum columns for all entity lifecycle tracking.

### Pattern

Instead of:
```csharp
public BookingStatus Status { get; set; } // enum
```

We use:
```csharp
public DateTimeOffset? ConfirmedAtUtc { get; set; }
public Guid? ConfirmedBy { get; set; }

public DateTimeOffset? CancelledAtUtc { get; set; }
public Guid? CancelledBy { get; set; }
```

### Deriving Status

Status is derived from timestamps, not stored. Each entity exposes a computed property or method:

```csharp
// Example: Booking
// CancelledAtUtc != null → Cancelled
// CompletedAtUtc != null → Completed
// StartedAtUtc != null   → InProgress
// ConfirmedAtUtc != null → Confirmed
// else                   → Scheduled
```

The **order of checks matters** — terminal states (Cancelled, Completed, NoShow) take precedence.

### Conventions

- All timestamp fields end with `AtUtc` suffix (e.g. `CreatedAtUtc`, `SentAtUtc`)
- Each timestamp is paired with a `By` field (Guid, references the user who performed the action)
- `CreatedAtUtc` / `CreatedBy` are required on all entities
- `UpdatedAtUtc` / `UpdatedBy` are optional (null until first update)
- Classification enums (e.g. `NotificationChannel`, `StaffRole`) are **not** affected by this ADR — they represent type, not lifecycle

### Querying

For database queries that filter by "status", use the timestamp columns directly:
```sql
-- All confirmed bookings not yet completed
WHERE confirmed_at_utc IS NOT NULL AND completed_at_utc IS NULL
```

## Consequences

- **Positive:** Built-in audit trail — you always know when and by whom each state change happened.
- **Positive:** No risk of status column getting out of sync with reality.
- **Positive:** More expressive queries — "confirmed after 5pm" is trivial, impossible with a status enum.
- **Positive:** No need for a separate audit/history table for state transitions.
- **Negative:** Querying "current status" requires checking multiple columns (mitigated by computed properties and indexed views).
- **Negative:** Adding a new state means adding a new nullable column (migration required).
- **Negative:** Developers must understand the precedence rules for deriving status.
