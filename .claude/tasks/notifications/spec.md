# Notifications (Meldingen)

## Overview

Chairly automatically sends email notifications to clients when key booking lifecycle events occur: a booking is created (confirmation), a booking is upcoming (reminder 24 hours in advance), or a booking is cancelled. Notifications are triggered by domain events published to RabbitMQ from the Bookings context, consumed by a dedicated hosted service in the Notifications context, and dispatched via SMTP. The full notification log is visible to Owners and Managers via a read-only page. This feature belongs to the Notifications bounded context.

---

## Domain Context

- Bounded context: Notifications
- Key entities involved: `Notification` (Aggregate Root)
- Related contexts: Bookings (source of domain events), Clients (recipient email lookup)
- Ubiquitous language:
  - Notification -- a single outbound message triggered by a booking event
  - BookingConfirmation -- email sent to the client when a booking is created or confirmed
  - BookingReminder -- email sent to the client 24 hours before the booking start time
  - BookingCancellation -- email sent to the client when a booking is cancelled

### Entity

**`Notification`** (Aggregate Root)

| Field | Type | Notes |
|---|---|---|
| `Id` | Guid | PK |
| `TenantId` | Guid | Multi-tenant scope |
| `RecipientId` | Guid | `ClientId` or `StaffMemberId` |
| `RecipientType` | RecipientType | `Client` | `StaffMember` |
| `Channel` | NotificationChannel | `Email` | `Sms` |
| `Type` | NotificationType | `BookingConfirmation` | `BookingReminder` | `BookingCancellation` |
| `ReferenceId` | Guid | `BookingId` this notification relates to |
| `ScheduledAtUtc` | DateTimeOffset | When to dispatch (immediate = `CreatedAtUtc`; reminder = `StartTime - 24h`) |
| `CreatedAtUtc` | DateTimeOffset | Required |
| `SentAtUtc` | DateTimeOffset? | Set on successful delivery |
| `FailedAtUtc` | DateTimeOffset? | Set after all retries exhausted |
| `FailureReason` | string? | Last error detail |
| `RetryCount` | int | Number of send attempts so far (default 0) |

**Derived status (no status column - ADR-009):**

| Status | Condition |
|---|---|
| Wachtend (Pending) | `SentAtUtc` and `FailedAtUtc` both null |
| Verzonden (Sent) | `SentAtUtc` set |
| Mislukt (Failed) | `FailedAtUtc` set |

**Enums (classification, not status -- kept as enums per domain model):**
- `RecipientType`: `Client`, `StaffMember`
- `NotificationChannel`: `Email`, `Sms`
- `NotificationType`: `BookingConfirmation`, `BookingReminder`, `BookingCancellation`

### Business rules

- Notifications are never created manually -- always triggered by domain events.
- BookingConfirmation: created immediately (`ScheduledAtUtc = CreatedAtUtc`) when a booking is created or confirmed.
- BookingReminder: created when a booking is created or confirmed; `ScheduledAtUtc = booking.StartTime - 24 hours`.
- BookingCancellation: created immediately when a booking is cancelled.
- If a booking is cancelled before the reminder `ScheduledAtUtc` is reached, the pending reminder is voided (set `FailedAtUtc`, `FailureReason = "Booking cancelled"`).
- Retry: up to 3 attempts total. After each failure, increment `RetryCount`. After the 3rd failure, set `FailedAtUtc` and `FailureReason`.
- Only Email channel in this iteration; `Channel` is always `Email` for client notifications.
- Recipient email is looked up at dispatch time from the Client record (not cached on the `Notification`).

---

## Backend Tasks

### B1 - Notification entity, EF configuration, and migration

Create `Notification` entity in Chairly.Domain and EF configuration in Chairly.Infrastructure. Add enums for `RecipientType`, `NotificationChannel`, `NotificationType` (these may already exist as stubs -- check first).

**Domain -- `Chairly.Domain/Entities/Notification.cs`:**

```csharp
public class Notification
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid RecipientId { get; set; }
    public RecipientType RecipientType { get; set; }
    public NotificationChannel Channel { get; set; }
    public NotificationType Type { get; set; }
    public Guid ReferenceId { get; set; }
    public DateTimeOffset ScheduledAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? SentAtUtc { get; set; }
    public DateTimeOffset? FailedAtUtc { get; set; }
    public string? FailureReason { get; set; }
    public int RetryCount { get; set; }
}
```

**Domain enums** (in `Chairly.Domain/Enums/` if not present):

```csharp
public enum RecipientType { Client, StaffMember }
public enum NotificationChannel { Email, Sms }
public enum NotificationType { BookingConfirmation, BookingReminder, BookingCancellation }
```

**EF Configuration -- `Chairly.Infrastructure/Configurations/NotificationConfiguration.cs`:**

- Table: `Notifications`
- Index on `(TenantId, ScheduledAtUtc)` -- for dispatcher polling query
- Index on `(TenantId, ReferenceId)` -- for cancellation void lookup
- Index on `(TenantId, CreatedAtUtc DESC)` -- for log list ordering
- `FailureReason` max length 1000
- `RetryCount` default value 0

**Migration:** Add and apply migration `AddNotifications`.

---

### B2 - Booking domain events published to RabbitMQ

Add domain event publishing to the existing Bookings handlers. Domain events are plain C# records published after a successful write.

**Domain events** (`Chairly.Domain/Events/`):

```csharp
public record BookingCreatedEvent(Guid TenantId, Guid BookingId, Guid ClientId, DateTimeOffset StartTime);
public record BookingConfirmedEvent(Guid TenantId, Guid BookingId, Guid ClientId, DateTimeOffset StartTime);
public record BookingCancelledEvent(Guid TenantId, Guid BookingId, Guid ClientId);
```

**RabbitMQ publisher** (`Chairly.Infrastructure/Messaging/BookingEventPublisher.cs`):

- Interface: `IBookingEventPublisher` with methods `PublishCreated`, `PublishConfirmed`, `PublishCancelled`
- Implementation serialises the event to JSON and publishes to exchange `chairly.bookings` with routing keys:
  - `booking.created`
  - `booking.confirmed`
  - `booking.cancelled`
- Exchange type: `topic`, durable
- Use `RabbitMQ.Client` (already available via Aspire)

**Aspire wiring** (`Chairly.AppHost/Program.cs`):

- Declare RabbitMQ exchange `chairly.bookings` and queue `notifications.bookings` with binding `booking.*`
- Inject connection string into `Chairly.Api` via Aspire resource reference

**Handler changes:**

- `CreateBookingHandler`: inject `IBookingEventPublisher`, publish `BookingCreatedEvent` after successful save
- `ConfirmBookingHandler`: publish `BookingConfirmedEvent` after successful confirm
- `CancelBookingHandler`: publish `BookingCancelledEvent` after successful cancel

**Tests:**
- Unit test: `CreateBookingHandler` calls `PublishCreated` on success
- Unit test: publisher not called when handler returns validation error
- Use a mock `IBookingEventPublisher` in existing handler tests

---

### B3 - RabbitMQ consumer and Notification record creation

A hosted service (`IHostedService`) that consumes booking events from `notifications.bookings` and creates `Notification` records in the database.

**Location:** `Chairly.Api/Features/Notifications/Infrastructure/BookingEventConsumer.cs`

**Behaviour:**

1. On startup, connect to RabbitMQ and subscribe to `notifications.bookings`.
2. For each message, deserialise to the appropriate event type by routing key.
3. Dispatch to the corresponding handler:

   - `booking.created` / `booking.confirmed` -- create two notifications:
     1. `BookingConfirmation` -- `ScheduledAtUtc = now`, `Channel = Email`, `RecipientType = Client`
     2. `BookingReminder` -- `ScheduledAtUtc = StartTime - 24h`, same channel/recipient
   - `booking.cancelled` -- create one notification:
     1. `BookingCancellation` -- `ScheduledAtUtc = now`
     2. Void any pending `BookingReminder` for the same `ReferenceId`: set `FailedAtUtc = now`, `FailureReason = "Boeking geannuleerd"`

4. Acknowledge message after successful DB write.
5. On exception, NACK without requeue (message goes to dead-letter if configured; log the error).

**Tests:**
- Consumer creates correct notification records for each event type
- Consumer voids pending reminder on cancellation event
- Consumer handles unknown routing key gracefully (ack + log, no exception)

---

### B4 - Email dispatch service and templates

SMTP email sending with inline HTML templates per notification type.

**Interface** (`Chairly.Api/Features/Notifications/Infrastructure/IEmailSender.cs`):

```csharp
public interface IEmailSender
{
    Task SendAsync(string toEmail, string toName, string subject, string htmlBody, CancellationToken ct);
}
```

**Implementation** (`SmtpEmailSender.cs`):

- Uses `System.Net.Mail.SmtpClient` (or `MailKit` -- prefer `MailKit` for async support; add NuGet `MailKit`)
- Configuration via `IOptions<SmtpSettings>`:
  ```csharp
  public class SmtpSettings
  {
      public string Host { get; set; } = string.Empty;
      public int Port { get; set; } = 587;
      public string Username { get; set; } = string.Empty;
      public string Password { get; set; } = string.Empty;
      public string FromAddress { get; set; } = string.Empty;
      public string FromName { get; set; } = string.Empty;
  }
  ```
- Register via `builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("Smtp"))`
- Aspire: add SMTP connection string / env vars in `Chairly.AppHost`

**Email templates** (`Chairly.Api/Features/Notifications/Infrastructure/EmailTemplates.cs`):

Static methods returning `(string Subject, string HtmlBody)` tuples. Inline HTML -- no template engine. Supported Dutch templates:

- `BookingConfirmation(string clientName, DateTimeOffset startTime, string serviceSummary)`:
  - Subject: "Bevestiging van uw afspraak bij {salonName}"
  - Body: greeting, date/time of appointment, list of services, footer
- `BookingReminder(string clientName, DateTimeOffset startTime, string serviceSummary)`:
  - Subject: "Herinnering: uw afspraak morgen bij {salonName}"
  - Body: reminder text, date/time, services, footer
- `BookingCancellation(string clientName, DateTimeOffset startTime)`:
  - Subject: "Uw afspraak is geannuleerd"
  - Body: cancellation notice, original date/time, footer

For this iteration `salonName` is a placeholder "Uw salon" -- tenant settings are out of scope.

**Tests:**
- Unit test: `SmtpEmailSender` calls SMTP client with correct to/from/subject (mock SMTP)
- Unit test: each template returns non-empty subject and body containing key field values

---

### B5 - Notification dispatcher background service

A background service (`BackgroundService`) that polls for pending notifications, dispatches them via email, and handles retries.

**Location:** `Chairly.Api/Features/Notifications/Infrastructure/NotificationDispatcher.cs`

**Behaviour (polling loop, every 60 seconds):**

1. Query `Notifications` where `SentAtUtc IS NULL AND FailedAtUtc IS NULL AND ScheduledAtUtc <= UtcNow AND RetryCount < 3`.
2. For each pending notification (process in batches of 50):
   a. Look up recipient email: if `RecipientType = Client`, load `Client` by `RecipientId`; use `Client.Email`. Skip (log warning) if no email address.
   b. Look up booking details (StartTime, service summary) by `ReferenceId`.
   c. Render the appropriate template using `EmailTemplates`.
   d. Call `IEmailSender.SendAsync`.
   e. On success: set `SentAtUtc = UtcNow`.
   f. On exception: increment `RetryCount`. If `RetryCount >= 3`: set `FailedAtUtc = UtcNow`, `FailureReason = exception.Message`.
3. Save changes after each notification (not batched, so partial progress is preserved).
4. Log a summary after each poll cycle.

**Tests:**
- Unit test: dispatcher sets `SentAtUtc` on successful send
- Unit test: dispatcher increments `RetryCount` on failure
- Unit test: dispatcher sets `FailedAtUtc` after 3 failures
- Unit test: dispatcher skips client with no email address

---

### B6 - Get notification list endpoint

**Slice:** `Chairly.Api/Features/Notifications/GetNotificationsList/`

**Route:** `GET /api/notifications`

**Authorisation:** Owner or Manager only. StaffMember returns `403`.

**Handler logic:**

1. Return all notifications for tenant ordered by `CreatedAtUtc` descending.
2. Join to `Client` (when `RecipientType = Client`) to resolve `RecipientName` (`FirstName + " " + LastName`).
3. Compute `status` string from timestamps.

**Response body:**

```json
[
  {
    "id": "guid",
    "type": "BookingConfirmation|BookingReminder|BookingCancellation",
    "recipientName": "string",
    "channel": "Email",
    "status": "Wachtend|Verzonden|Mislukt",
    "scheduledAtUtc": "datetime",
    "sentAtUtc": "datetime?",
    "failedAtUtc": "datetime?",
    "failureReason": "string?",
    "retryCount": 0,
    "referenceId": "guid"
  }
]
```

**Tests:**
- Returns empty list when no notifications
- Returns correct `status` strings for each state
- Returns `403` for StaffMember callers
- Ordered newest first

---

## Frontend Tasks

### F1 - Notification models and API service

**Location:** `libs/chairly/src/lib/notifications/`

**Models** (`models/notification.model.ts`):

```typescript
export type NotificationType = 'BookingConfirmation' | 'BookingReminder' | 'BookingCancellation';
export type NotificationChannel = 'Email' | 'Sms';
export type NotificationStatus = 'Wachtend' | 'Verzonden' | 'Mislukt';

export interface NotificationSummary {
  id: string;
  type: NotificationType;
  recipientName: string;
  channel: NotificationChannel;
  status: NotificationStatus;
  scheduledAtUtc: string;
  sentAtUtc?: string;
  failedAtUtc?: string;
  failureReason?: string;
  retryCount: number;
  referenceId: string;
}
```

**API service** (`data-access/notifications.service.ts`):

```typescript
// Methods:
getNotifications(): Observable<NotificationSummary[]>
```

**Dutch type labels** (pure utility function in `util/notification-labels.ts`):

```typescript
export function notificationTypeLabel(type: NotificationType): string {
  // 'BookingConfirmation' -> 'Bevestiging'
  // 'BookingReminder'     -> 'Herinnering'
  // 'BookingCancellation' -> 'Annulering'
}
```

---

### F2 - Notification log page

**Location:** `libs/chairly/src/lib/notifications/feature/notification-log/`

**Route:** `/meldingen` (lazy-loaded, Owner/Manager only -- add route guard)

**Smart component:** `NotificationLogPageComponent`

Loads notifications via `NotificationsService.getNotifications()` on init. Auto-refreshes every 30 seconds using `interval(30_000)` + `takeUntilDestroyed(destroyRef)`.

**Template (`notification-log-page.component.html`):**

- Page heading: "Meldingen"
- Loading state while fetching
- Empty state: "Nog geen meldingen verstuurd"
- Table columns:
  - Type -- Dutch label via `notificationTypeLabel` pipe
  - Ontvanger -- recipient name
  - Kanaal -- "E-mail"
  - Status -- badge:
    - Wachtend -> gray
    - Verzonden -> green
    - Mislukt -> red
  - Gepland op -- formatted Dutch date (`d MMM yyyy HH:mm`)
  - Verzonden op -- formatted Dutch date, or `--` if not sent
- Mislukt rows: show `failureReason` as a tooltip or collapsed detail row

**Route registration** in `notifications.routes.ts` at the notifications domain root:

```typescript
{ path: 'meldingen', component: NotificationLogPageComponent }
```

Add "Meldingen" nav item to sidebar (Owner/Manager only).

**`notificationTypeLabel` pipe** (`pipes/notification-type-label.pipe.ts`):

An Angular `@Pipe` that transforms `NotificationType` to Dutch label string. Use this in the template instead of a function call.

**Playwright e2e (`apps/chairly-e2e/src/notifications.spec.ts`):**

- Navigate to `/meldingen`
- Verify empty state when no notifications exist
- (Integration scenario -- may require a seeded booking) After creating a booking, navigate to `/meldingen` and verify a "Bevestiging" row with status "Wachtend" or "Verzonden" appears

---

## Acceptance Criteria

- [ ] `Notification` entity exists in Chairly.Domain with correct fields and enums
- [ ] EF configuration with indexes on `(TenantId, ScheduledAtUtc)` and `(TenantId, ReferenceId)`
- [ ] `BookingCreatedEvent`, `BookingConfirmedEvent`, `BookingCancelledEvent` published to RabbitMQ from Bookings handlers
- [ ] RabbitMQ consumer creates correct `Notification` records for each event type
- [ ] Pending `BookingReminder` is voided when a `BookingCancelled` event arrives for the same booking
- [ ] `SmtpEmailSender` sends emails via MailKit with correct subject and HTML body
- [ ] Dutch email templates exist for all three notification types
- [ ] `NotificationDispatcher` polls every 60 seconds and dispatches pending notifications
- [ ] Retry logic: up to 3 attempts, then sets `FailedAtUtc`
- [ ] `GET /api/notifications` returns list with derived status, newest first (Owner/Manager only)
- [ ] Notification log page at `/meldingen` with status badges and Dutch type labels
- [ ] Page auto-refreshes every 30 seconds
- [ ] "Meldingen" nav item visible to Owner/Manager only
- [ ] All backend quality checks pass (dotnet build, test, format)
- [ ] All frontend quality checks pass (lint, test, build)
- [ ] Playwright e2e tests pass

---

## Out of Scope

- SMS channel (Channel = Sms exists in the enum but no dispatch logic)
- Manual notification resend
- Staff member notifications (only client notifications in this iteration)
- Tenant-specific sender name / salon name in email templates
- HTML email template engine (inline HTML strings only)
- Unsubscribe / opt-out for clients
- Notification preferences per client
- Webhook delivery channel
- Dead-letter queue handling in RabbitMQ
- Pagination and filtering on the notification log
