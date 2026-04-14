# Newsletter

## Overview

Allow Owners and Managers to compose and broadcast marketing emails to all clients of the tenant who have a non-empty email address and have not unsubscribed. Newsletters live in their own `Newsletters` bounded context separate from transactional `Notifications`. Composition uses a rich text editor (Quill); HTML is sanitized server-side. Sending is performed asynchronously through a dedicated RabbitMQ worker that fans out per-recipient send events to the existing email infrastructure. Per-recipient delivery is tracked, and recipients can unsubscribe through a tokenized public link in the email footer.

## Domain Context

- Bounded context: **Newsletters** (new). Reuses SMTP and RabbitMQ infrastructure but has its own aggregate, slices, and frontend domain library.
- Key entities involved:
  - `NewsletterCampaign` (new aggregate root)
  - `NewsletterDelivery` (new child entity, one row per recipient)
  - `Client` (existing — extended with `IsSubscribedToNewsletter` flag)
  - `TenantSettings` (read-only, used for `CompanyName` and footer info in the rendered email)
- Ubiquitous language:
  - **Newsletter Campaign** — a single broadcast prepared by the salon, identified by a subject and rich-text body.
  - **Newsletter Delivery** — the per-recipient record describing whether the campaign was sent, failed, or unsubscribed.
  - **Recipient Filter** — the rule used to materialise the recipient list at send time. MVP value: `AllSubscribed`.
  - **Subscribed Client** — a `Client` with non-empty `Email` and `IsSubscribedToNewsletter = true`.
  - **Campaign Status** (derived, never stored as enum column):
    - `Draft` — `SentAtUtc IS NULL` and `ScheduledAtUtc IS NULL` and `CancelledAtUtc IS NULL`
    - `Scheduled` — `ScheduledAtUtc IS NOT NULL` and `SentAtUtc IS NULL` and `CancelledAtUtc IS NULL`
    - `Sending` — `QueuedAtUtc IS NOT NULL` and `SentAtUtc IS NULL` and `CancelledAtUtc IS NULL`
    - `Sent` — `SentAtUtc IS NOT NULL`
    - `Cancelled` — `CancelledAtUtc IS NOT NULL` and `SentAtUtc IS NULL`

### Access Control

- `Owner` and `Manager` may list, create, edit, send, schedule, cancel, preview, and test-send newsletter campaigns.
- `StaffMember` cannot access any newsletter endpoint or page.
- The unsubscribe endpoint is **public** (no auth) and uses a per-delivery opaque token.

### Business Rules

- A campaign starts as `Draft`. The composer may save freely.
- Sending or scheduling a campaign requires non-empty `Subject` and non-empty `BodyHtml` (after sanitisation).
- HTML body is sanitised server-side using `Ganss.Xss` (HtmlSanitizer NuGet) before persistence. Stored HTML is the sanitised form.
- A campaign that is `Sent` or `Cancelled` is read-only; updates and deletes are rejected.
- **Deletion rules:** Only campaigns in `Draft` or `Scheduled` status may be deleted. `Sending`, `Sent`, and `Cancelled` campaigns are read-only — delete requests on these statuses must be rejected with `409 Conflict`.
- "Send now" creates the `NewsletterDelivery` rows for all currently subscribed clients with email, sets `QueuedAtUtc`, and publishes a `NewsletterCampaignQueued` domain event. Recipients are snapshotted at queue time.
- "Schedule" stores `ScheduledAtUtc`. A hosted background worker polls due campaigns once per minute and queues them via the same publisher path.
- Cancelling a `Scheduled` campaign sets `CancelledAtUtc`/`CancelledBy` and prevents queueing.
- Cancelling a `Sending` campaign sets `CancelledAtUtc`/`CancelledBy`; deliveries already enqueued are not retracted, but pending deliveries skip sending.
- Test-send sends a single email rendered with the current draft to the email address on the authenticated user's claims (does **not** create deliveries).
- Unsubscribe via tokenized link: sets `UnsubscribedAtUtc` on the delivery row and flips `Client.IsSubscribedToNewsletter` to `false`. Idempotent.

---

## Backend Tasks

### B1 — Add `IsSubscribedToNewsletter` to Client, migration, and domain-model diagram update

Extend the existing `Client` entity with a newsletter opt-in flag.

**Domain — `Chairly.Domain/Entities/Client.cs`:**
- Add `public bool IsSubscribedToNewsletter { get; set; } = true;`

**EF Configuration — `ClientConfiguration.cs`:**
- Map `IsSubscribedToNewsletter` as `bool NOT NULL DEFAULT TRUE`.

**Migration:** `AddClientNewsletterSubscription` — must be idempotent per CLAUDE.md:
- Use a `DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Clients' AND column_name = 'IsSubscribedToNewsletter') THEN ALTER TABLE "Clients" ADD COLUMN "IsSubscribedToNewsletter" boolean NOT NULL DEFAULT TRUE; END IF; END $$;` block.

**Domain model documentation:** Update `docs/domain-model.md` `Clients` section to mention `IsSubscribedToNewsletter` and reference the new Newsletters context. Also update the entity relationship diagram in `docs/domain-model.md` to reflect the new `IsSubscribedToNewsletter` attribute on `Client` under the Tenant tree.

**Tests:**
- Existing clients default to `IsSubscribedToNewsletter = true` after migration (integration test asserts seed clients).
- Updating the flag persists correctly.

---

### B2 — `NewsletterCampaign` and `NewsletterDelivery` entities (with `CreatedBy` on delivery), EF config, migration, domain-model diagram update

Create the aggregate and per-recipient delivery row.

**Domain — `Chairly.Domain/Entities/NewsletterCampaign.cs`:**
```csharp
public class NewsletterCampaign
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string BodyHtml { get; set; } = string.Empty;
    public NewsletterRecipientFilter RecipientFilter { get; set; } = NewsletterRecipientFilter.AllSubscribed;

    public DateTimeOffset? ScheduledAtUtc { get; set; }
    public Guid? ScheduledBy { get; set; }
    public DateTimeOffset? QueuedAtUtc { get; set; }
    public Guid? QueuedBy { get; set; }
    public DateTimeOffset? SentAtUtc { get; set; }
    public Guid? SentBy { get; set; }
    public DateTimeOffset? CancelledAtUtc { get; set; }
    public Guid? CancelledBy { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid? UpdatedBy { get; set; }

    public List<NewsletterDelivery> Deliveries { get; set; } = new();
}
```

**Domain — `Chairly.Domain/Entities/NewsletterDelivery.cs`:**
```csharp
public class NewsletterDelivery
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid CampaignId { get; set; }
    public Guid ClientId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string UnsubscribeToken { get; set; } = string.Empty;

    public DateTimeOffset? SentAtUtc { get; set; }
    public DateTimeOffset? FailedAtUtc { get; set; }
    public string? FailureReason { get; set; }
    public DateTimeOffset? UnsubscribedAtUtc { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid CreatedBy { get; set; }
}
```

**Domain — `Chairly.Domain/Enums/NewsletterRecipientFilter.cs`:**
```csharp
public enum NewsletterRecipientFilter { AllSubscribed = 1 }
```

**EF Configuration — `Chairly.Infrastructure/Persistence/Configurations/`:**
- `NewsletterCampaignConfiguration`:
  - Table: `NewsletterCampaigns`
  - `Subject` max length 500
  - `BodyHtml` `text` (no max length)
  - `RecipientFilter` stored as int
  - `ScheduledBy` mapped as nullable `Guid` (timestamp pair with `ScheduledAtUtc` per ADR-009)
  - Index on `(TenantId, ScheduledAtUtc)` for the scheduler poll
  - Index on `(TenantId, CreatedAtUtc DESC)` for list paging
  - HasMany `Deliveries` with cascade delete
- `NewsletterDeliveryConfiguration`:
  - Table: `NewsletterDeliveries`
  - `Email` max length 320
  - `UnsubscribeToken` max length 64, **unique index** globally (so the public unsubscribe endpoint can resolve without tenant)
  - `FailureReason` max length 1000, nullable
  - Index on `(TenantId, CampaignId)`
  - FK to `Clients(Id)` with restrict (do not cascade-delete deliveries when a client is removed)
  - Map `CreatedAtUtc` and `CreatedBy` (Guid, required) per CLAUDE.md's required `CreatedAtUtc`/`CreatedBy` pair on all entities
- Add `DbSet<NewsletterCampaign> NewsletterCampaigns` and `DbSet<NewsletterDelivery> NewsletterDeliveries` to `ChairlyDbContext`.

**Migration:** `AddNewsletters` — fully idempotent (raw SQL `CREATE TABLE IF NOT EXISTS`, `CREATE INDEX IF NOT EXISTS`, `CREATE UNIQUE INDEX IF NOT EXISTS`). The `NewsletterCampaigns` table must include the `ScheduledBy` nullable `uuid` column alongside `ScheduledAtUtc`.

**Domain model documentation:** Add a new "Newsletters" section to `docs/domain-model.md` listing `NewsletterCampaign`, `NewsletterDelivery`, and the derived status rules. Also update the entity relationship diagram in `docs/domain-model.md` to include `NewsletterCampaign` and `NewsletterDelivery` under the Tenant tree, showing the relationship from `NewsletterCampaign` to `NewsletterDelivery` and from `NewsletterDelivery` to `Client`.

**Tests:**
- Round-trip persistence of `NewsletterCampaign` with deliveries.
- Unique index on `UnsubscribeToken` enforced.
- Index on `(TenantId, ScheduledAtUtc)` exists.

---

### B3 — HTML sanitisation helper

Add the `HtmlSanitizer` NuGet package (`Ganss.Xss`) to `Chairly.Api`. Wrap it in a small service so handlers do not depend on the package directly.

**File:** `Chairly.Api/Features/Newsletters/Infrastructure/INewsletterHtmlSanitizer.cs` and `NewsletterHtmlSanitizer.cs`.

```csharp
internal interface INewsletterHtmlSanitizer
{
    string Sanitize(string html);
}
```

Implementation configures the allowed tags/attributes for marketing email content (headings, paragraphs, lists, links with `href`, images with `src`/`alt`, basic inline formatting). Strips `<script>`, `<style>`, `on*` handlers, `javascript:` URLs.

Register as singleton in `Program.cs`.

**Tests:**
- Strips `<script>` tags.
- Strips `onclick` handlers.
- Preserves `<a href="https://...">`, `<p>`, `<strong>`, `<ul>/<li>`, `<img src="https://...">`.
- Empty/whitespace input returns empty string.

---

### B4 — `INewsletterEventPublisher` contract and RabbitMQ implementation

Mirror the `IBookingEventPublisher` pattern.

**File:** `Chairly.Infrastructure/Messaging/INewsletterEventPublisher.cs`
```csharp
public interface INewsletterEventPublisher
{
    Task PublishCampaignQueuedAsync(NewsletterCampaignQueuedEvent @event, CancellationToken ct);
    Task PublishDeliveryRequestedAsync(NewsletterDeliveryRequestedEvent @event, CancellationToken ct);
    Task PublishTestRequestedAsync(NewsletterTestRequestedEvent @event, CancellationToken ct);
}
```

**Events:**
- `NewsletterCampaignQueuedEvent { TenantId, CampaignId, QueuedAtUtc }`
- `NewsletterDeliveryRequestedEvent { TenantId, CampaignId, DeliveryId }`
- `NewsletterTestRequestedEvent { TenantId, CampaignId, RecipientEmail, RequestedBy }`

**Implementation:** `NewsletterEventPublisher` publishes to RabbitMQ exchanges:
- `chairly.newsletter.campaign-queued`
- `chairly.newsletter.delivery-requested`
- `chairly.newsletter.test-requested`

Register in `Program.cs`. Provide a `NullNewsletterEventPublisher` test double in `Chairly.Tests/Helpers/`.

**Tests:**
- `NullNewsletterEventPublisher` records calls (Recording variant) for handler tests.
- Integration test publishes to the real RabbitMQ broker started by Aspire/Testcontainers (mirroring existing booking publisher tests).

---

### B5 — Create newsletter campaign endpoint

**Slice:** `Chairly.Api/Features/Newsletters/CreateNewsletterCampaign/`

**POST /api/newsletters/campaigns:**
- Command: `CreateNewsletterCampaignCommand` implementing `IRequest<OneOf<NewsletterCampaignResponse, ValidationError>>`
  - `Subject` `[Required] [MaxLength(500)]`
  - `BodyHtml` `[Required]`
- Handler:
  1. Sanitise `BodyHtml` via `INewsletterHtmlSanitizer`.
  2. Validate sanitised body is not empty after stripping HTML/whitespace.
  3. Create `NewsletterCampaign` with `CreatedAtUtc`/`CreatedBy` from current user.
  4. Persist, return `NewsletterCampaignResponse`.
- Access: Owner, Manager.
- Returns `201 Created` with location header.

**Tests:**
- Persists sanitised HTML (script tag stripped).
- Returns 422 on empty subject.
- Returns 422 when sanitised body is empty.
- Returns 403 for staff member.

---

### B6 — Update newsletter campaign endpoint

**Slice:** `Chairly.Api/Features/Newsletters/UpdateNewsletterCampaign/`

**PUT /api/newsletters/campaigns/{id}:**
- Command: `UpdateNewsletterCampaignCommand` with same fields as B5 plus `Id` from route.
- Handler:
  1. Load campaign by `(Id, TenantId)` — 404 if missing.
  2. Reject (`409 Conflict`) if `SentAtUtc` or `CancelledAtUtc` or `QueuedAtUtc` is set.
  3. Sanitise body, update fields, set `UpdatedAtUtc`/`UpdatedBy`.
  4. Return updated response.
- Access: Owner, Manager.

**Tests:**
- Updates draft successfully.
- 409 for sent campaign.
- 409 for cancelled campaign.
- 404 for unknown id / wrong tenant.

---

### B7 — List newsletter campaigns endpoint

**Slice:** `Chairly.Api/Features/Newsletters/GetNewsletterCampaignsList/`

**GET /api/newsletters/campaigns:**
- Query: `GetNewsletterCampaignsListQuery` (no params for MVP, sorted by `CreatedAtUtc DESC`).
- Handler returns `List<NewsletterCampaignSummaryResponse>`:
  - `Id`, `Subject`, `Status` (string, derived from timestamps), `RecipientCount` (count of deliveries), `SentCount`, `FailedCount`, `ScheduledAtUtc`, `SentAtUtc`, `CreatedAtUtc`.
- Access: Owner, Manager.

**Tests:**
- Returns campaigns ordered by CreatedAtUtc desc.
- Status derivation matches each lifecycle.
- Counts include sent/failed/total deliveries.
- 403 for staff.

---

### B8 — Get newsletter campaign detail endpoint

**Slice:** `Chairly.Api/Features/Newsletters/GetNewsletterCampaignDetail/`

**GET /api/newsletters/campaigns/{id}:**
- Query: `GetNewsletterCampaignDetailQuery`
- Returns `NewsletterCampaignDetailResponse` with all campaign fields, derived `Status`, recipient counts (`Total`, `Sent`, `Failed`, `Pending`, `Unsubscribed`), and the projected eligible recipient count for currently subscribed clients (used pre-send).
- 404 if unknown.
- Access: Owner, Manager.

**Tests:**
- Returns full detail.
- Counts match deliveries when sent.
- Eligible-recipient projection matches subscribed client count when in draft.

---

### B9 — Delete newsletter campaign endpoint (Draft and Scheduled only; 409 for Sending/Sent/Cancelled)

**Slice:** `Chairly.Api/Features/Newsletters/DeleteNewsletterCampaign/`

**DELETE /api/newsletters/campaigns/{id}:**
- Allowed only when derived status is `Draft` or `Scheduled` (i.e. `QueuedAtUtc IS NULL && SentAtUtc IS NULL && CancelledAtUtc IS NULL`). No deliveries exist in those states.
- Returns `409 Conflict` for `Sending` (`QueuedAtUtc IS NOT NULL`), `Sent` (`SentAtUtc IS NOT NULL`), and `Cancelled` (`CancelledAtUtc IS NOT NULL`) — these are read-only per the Domain Context deletion rules.
- Cascade-deletes deliveries (none exist in Draft/Scheduled).
- Returns `204 No Content`.
- Access: Owner, Manager.

**Tests:**
- Deletes a `Draft` campaign.
- Deletes a `Scheduled` campaign.
- 409 for `Sending` campaigns.
- 409 for `Sent` campaigns.
- 409 for `Cancelled` campaigns.
- 404 for unknown.

---

### B10 — Schedule newsletter campaign endpoint

**Slice:** `Chairly.Api/Features/Newsletters/ScheduleNewsletterCampaign/`

**POST /api/newsletters/campaigns/{id}/schedule:**
- Command: `ScheduleNewsletterCampaignCommand { Id, ScheduledAtUtc }`.
- Validation: `ScheduledAtUtc` must be in the future (at least 1 minute ahead).
- Handler:
  1. Load campaign — 404 if missing.
  2. 409 if campaign is not in `Draft` status (i.e. `QueuedAtUtc`, `SentAtUtc`, or `CancelledAtUtc` is set).
  3. Validate Subject + sanitised BodyHtml are non-empty.
  4. Set `ScheduledAtUtc`, `ScheduledBy` (current user), `UpdatedAtUtc`, `UpdatedBy`.
- Access: Owner, Manager.

**Tests:**
- Schedules a draft.
- 422 if scheduled in the past.
- 409 if already sent / queued / cancelled.
- 422 if subject or body empty.

---

### B11 — Cancel newsletter campaign endpoint

**Slice:** `Chairly.Api/Features/Newsletters/CancelNewsletterCampaign/`

**POST /api/newsletters/campaigns/{id}/cancel:**
- Handler:
  1. Load campaign — 404 if missing.
  2. 409 if `SentAtUtc` is set.
  3. Set `CancelledAtUtc`, `CancelledBy`.
- Access: Owner, Manager.

**Tests:**
- Cancels draft, scheduled, sending campaigns.
- 409 for sent.
- 404 for unknown.

---

### B12 — Send newsletter campaign now endpoint

**Slice:** `Chairly.Api/Features/Newsletters/SendNewsletterCampaign/`

**POST /api/newsletters/campaigns/{id}/send:**
- Handler:
  1. Load campaign — 404 if missing.
  2. 409 if not `Draft` or `Scheduled`.
  3. Validate Subject + sanitised BodyHtml non-empty (422 otherwise).
  4. Materialise recipients: query `Clients` for `TenantId == current && IsSubscribedToNewsletter == true && Email != null && Email != ""`.
  5. For each recipient, create a `NewsletterDelivery` with a fresh `UnsubscribeToken` (`Convert.ToHexString(RandomNumberGenerator.GetBytes(32))`).
  6. Set `QueuedAtUtc` and `QueuedBy`. Clear `ScheduledAtUtc`.
  7. SaveChangesAsync.
  8. Publish `NewsletterCampaignQueuedEvent` via `INewsletterEventPublisher`.
  9. Return `202 Accepted` with the campaign detail.
- Access: Owner, Manager.

**Tests:**
- Materialises only subscribed clients with non-empty email.
- Creates one delivery per recipient with a unique token.
- Publishes the queued event exactly once.
- 409 for already sent / cancelled campaign.
- 422 when no eligible recipients (return validation error "Geen ontvangers gevonden").

---

### B13 — Newsletter scheduler hosted service

**File:** `Chairly.Api/Features/Newsletters/Infrastructure/NewsletterSchedulerHostedService.cs`

A `BackgroundService` that polls every 60 seconds for due scheduled campaigns and triggers the same send pipeline as B12.

- Across all tenants, query `NewsletterCampaigns` where `ScheduledAtUtc <= now && QueuedAtUtc IS NULL && CancelledAtUtc IS NULL && SentAtUtc IS NULL`.
- For each, resolve a scoped `IMediator` and dispatch an internal `QueueScheduledNewsletterCommand` (which executes the same materialisation logic as B12, attributed to a system user `Guid.Empty`).
- Log skipped campaigns when subject/body is empty.
- Register in `Program.cs` via `AddHostedService`.

**Tests:**
- Picks up due campaigns and queues them.
- Skips not-yet-due campaigns.
- Skips cancelled campaigns.

---

### B14 — Newsletter send worker (RabbitMQ consumer)

**File:** `Chairly.Infrastructure/Messaging/NewsletterSendWorker.cs`

Consumes `chairly.newsletter.campaign-queued`. For each message:
1. Loads the campaign with its deliveries (filtered to those with `SentAtUtc IS NULL && FailedAtUtc IS NULL && UnsubscribedAtUtc IS NULL`).
2. Re-checks `CancelledAtUtc` — if set, ack and stop.
3. For each pending delivery, publishes `NewsletterDeliveryRequestedEvent` through `INewsletterEventPublisher`.
4. After all delivery requests are published, sets `SentAtUtc` and `SentBy = QueuedBy` on the campaign and saves.

A second consumer on `chairly.newsletter.delivery-requested`:
1. Loads delivery + campaign.
2. Renders the final HTML using a shared `NewsletterRenderer` that wraps the sanitised body in a layout including the salon name footer and the tokenized unsubscribe link `{publicBaseUrl}/api/newsletters/unsubscribe/{token}`.
3. Calls existing `IEmailSender` to deliver.
4. On success: set `SentAtUtc` on the delivery. On failure: set `FailedAtUtc` and `FailureReason`.

Handlers in slices must NEVER call `IEmailSender` directly — only the worker does.

**Tests:**
- Worker marks deliveries as sent on success.
- Worker marks deliveries as failed with reason on `IEmailSender` exception.
- Cancelled campaigns are skipped.
- Renderer includes unsubscribe link with token.

---

### B15 — Preview newsletter endpoint

**Slice:** `Chairly.Api/Features/Newsletters/PreviewNewsletter/`

**POST /api/newsletters/preview:**
- Command: `PreviewNewsletterCommand { Subject, BodyHtml }`.
- Handler sanitises body, renders the full HTML email via `NewsletterRenderer` using the tenant `CompanyName` and a placeholder unsubscribe link `#preview-unsubscribe`.
- Returns `PreviewNewsletterResponse { Subject, HtmlBody }`.
- Access: Owner, Manager.

**Tests:**
- Returns sanitised HTML wrapped in layout.
- Strips scripts.
- Includes salon name in footer.

---

### B16 — Test-send newsletter endpoint (publishes `NewsletterTestRequestedEvent`, no direct `IEmailSender`)

**Slice:** `Chairly.Api/Features/Newsletters/TestSendNewsletter/`

**POST /api/newsletters/campaigns/{id}/test-send:**
- Handler:
  1. Load campaign — 404 if missing.
  2. Resolve recipient email from current user's claims (`email` claim). 422 if no email on user.
  3. Publish a `NewsletterTestRequestedEvent { TenantId, CampaignId, RecipientEmail, RequestedBy }` via `INewsletterEventPublisher.PublishTestRequestedAsync(...)`.
  4. Return `202 Accepted`.
- Handler must **not** depend on `IEmailSender` — CLAUDE.md forbids direct `IEmailSender` calls in handlers without exception.
- Access: Owner, Manager.

**Event publisher extension:**
- Extend `INewsletterEventPublisher` in B4 with `Task PublishTestRequestedAsync(NewsletterTestRequestedEvent @event, CancellationToken ct);` routed to a new RabbitMQ exchange `chairly.newsletter.test-requested`.

**Worker extension (B14):**
- The newsletter send worker subscribes to `chairly.newsletter.test-requested`. On receipt it loads the campaign, renders the HTML via `NewsletterRenderer` (with a placeholder unsubscribe token `#test-send`) and calls `IEmailSender` to dispatch to `RecipientEmail`. No persistence row is written or mutated for test sends.

**Tests:**
- Publishes `NewsletterTestRequestedEvent` with correct email, campaign id and tenant.
- 422 if user has no email claim — no event published.
- 404 for unknown campaign — no event published.
- Handler has no direct `IEmailSender` dependency (constructor/DI assertion).
- Worker integration test: consuming the event triggers `IEmailSender` with the rendered body.

---

### B17 — Public unsubscribe endpoint

**Slice:** `Chairly.Api/Features/Newsletters/UnsubscribeNewsletter/`

**GET /api/newsletters/unsubscribe/{token}:**
- Anonymous (allows `[AllowAnonymous]`, no tenant middleware required because the token is globally unique).
- Handler:
  1. Find `NewsletterDelivery` by `UnsubscribeToken`. If none, return a Dutch HTML page "Ongeldige uitschrijflink" with `404`.
  2. Set `UnsubscribedAtUtc = DateTimeOffset.UtcNow` if not already set.
  3. Find the matching `Client` by `(TenantId, ClientId)` and set `IsSubscribedToNewsletter = false`.
  4. Save.
  5. Return a small Dutch HTML confirmation page "U bent uitgeschreven van onze nieuwsbrief."
- Idempotent: repeated visits return the same confirmation page.

**Tests:**
- Valid token unsubscribes client and marks delivery.
- Invalid token returns 404 page.
- Repeated calls remain idempotent.
- Endpoint is reachable without auth.

---

### B18 — Endpoint registration and module wiring

**File:** `Chairly.Api/Features/Newsletters/NewsletterEndpoints.cs`

Group all endpoints under `/api/newsletters` with the `RequireManagerOrOwner` policy except the public unsubscribe endpoint, which is mapped at the root with `[AllowAnonymous]`.

Register in `Program.cs`:
- `INewsletterHtmlSanitizer`
- `INewsletterEventPublisher` (real and test-double registrations for integration tests)
- `NewsletterSchedulerHostedService`
- `NewsletterSendWorker` (and delivery consumer) hosted services
- `MapNewsletterEndpoints()`

**Tests:**
- Endpoint discovery test asserts all newsletter routes are mapped.
- Auth integration tests for each route.

---

## Frontend Tasks

### F1 — Newsletter domain library scaffold (layer folders, Sheriff tag, empty routes file; no app-router or sidebar wiring)

Create a new domain library `libs/chairly/src/lib/newsletters/` following the standard layout:

```
newsletters/
├── data-access/
├── feature/
├── models/
├── ui/
├── util/
└── newsletters.routes.ts
```

- Add a Sheriff tag for `newsletters` in `sheriff.config.ts` mirroring the rules used by `notifications` (cannot import from other domains; can import from `shared`).
- Create empty layer folders (`data-access/`, `feature/`, `models/`, `ui/`, `util/`, `pipes/`) and the empty `newsletters.routes.ts` file at the domain root; the routes array itself is populated by F12.
- Scaffold barrel exports (`index.ts`) for each layer so later tasks can add files without re-touching exports structure.
- **Do not** touch the top-level app router — F12 is the sole owner of registering the `nieuwsbrief` route in the app routing config.
- **Do not** touch sidebar navigation — F13 is the sole owner of adding the "Nieuwsbrief" sidebar entry.

---

### F2 — Newsletter models

**Location:** `libs/chairly/src/lib/newsletters/models/`

Create TypeScript interfaces matching backend DTOs (camelCase):

- `newsletter-campaign-summary.model.ts` — `NewsletterCampaignSummary { id, subject, status, recipientCount, sentCount, failedCount, scheduledAtUtc?, sentAtUtc?, createdAtUtc }`
- `newsletter-campaign-detail.model.ts` — `NewsletterCampaignDetail { id, subject, bodyHtml, recipientFilter, status, scheduledAtUtc?, queuedAtUtc?, sentAtUtc?, cancelledAtUtc?, createdAtUtc, updatedAtUtc?, totalRecipients, sentCount, failedCount, pendingCount, unsubscribedCount, eligibleSubscribers }`
- `newsletter-status.model.ts` — string union `'Draft' | 'Scheduled' | 'Sending' | 'Sent' | 'Cancelled'`
- `create-newsletter-campaign-request.model.ts` — `{ subject, bodyHtml }`
- `update-newsletter-campaign-request.model.ts` — `{ subject, bodyHtml }`
- `schedule-newsletter-campaign-request.model.ts` — `{ scheduledAtUtc: string }`
- `preview-newsletter-request.model.ts` and `preview-newsletter-response.model.ts`

Create `index.ts` barrel export.

---

### F3 — Newsletter API service

**Location:** `libs/chairly/src/lib/newsletters/data-access/newsletters-api.service.ts`

Injectable, `providedIn: 'root'`. Methods:

- `listCampaigns(): Observable<NewsletterCampaignSummary[]>` — `GET /api/newsletters/campaigns`
- `getCampaign(id: string): Observable<NewsletterCampaignDetail>` — `GET /api/newsletters/campaigns/{id}`
- `createCampaign(request): Observable<NewsletterCampaignDetail>` — `POST /api/newsletters/campaigns`
- `updateCampaign(id, request): Observable<NewsletterCampaignDetail>` — `PUT /api/newsletters/campaigns/{id}`
- `deleteCampaign(id): Observable<void>` — `DELETE /api/newsletters/campaigns/{id}`
- `scheduleCampaign(id, request): Observable<NewsletterCampaignDetail>` — `POST /api/newsletters/campaigns/{id}/schedule`
- `cancelCampaign(id): Observable<NewsletterCampaignDetail>` — `POST /api/newsletters/campaigns/{id}/cancel`
- `sendCampaign(id): Observable<NewsletterCampaignDetail>` — `POST /api/newsletters/campaigns/{id}/send`
- `testSendCampaign(id): Observable<void>` — `POST /api/newsletters/campaigns/{id}/test-send`
- `previewNewsletter(request): Observable<PreviewNewsletterResponse>` — `POST /api/newsletters/preview`

Use the API base URL token already used by other services. Update barrel export.

---

### F4 — Newsletter SignalStore

**Location:** `libs/chairly/src/lib/newsletters/data-access/newsletter.store.ts`

NgRx SignalStore using `withState`, `withMethods`, `withComputed`.

**State:**
- `campaigns: NewsletterCampaignSummary[]`
- `selectedCampaign: NewsletterCampaignDetail | null`
- `isLoadingList: boolean`
- `isLoadingDetail: boolean`
- `isSaving: boolean`
- `isSending: boolean`
- `error: string | null`
- `preview: PreviewNewsletterResponse | null`
- `isLoadingPreview: boolean`

**Methods:**
- `loadCampaigns()`
- `loadCampaign(id: string)`
- `createCampaign(request)` — returns the new id via the rxMethod tap (for the route navigation)
- `updateCampaign(id, request)`
- `deleteCampaign(id)`
- `scheduleCampaign(id, request)`
- `cancelCampaign(id)`
- `sendCampaign(id)`
- `testSendCampaign(id)`
- `loadPreview(request)`
- `clearPreview()`

**Computed signals:**
- `draftCampaigns`, `scheduledCampaigns`, `sentCampaigns`, `cancelledCampaigns`

**Vitest tests:**
- Loading list toggles `isLoadingList` and populates `campaigns`.
- `createCampaign` adds to list and sets `selectedCampaign`.
- `sendCampaign` sets `isSending` and updates `selectedCampaign` on success.
- `scheduleCampaign` updates campaign with `ScheduledAtUtc`.
- `cancelCampaign` flips status to `Cancelled`.
- `loadPreview` populates preview signal.
- `clearPreview` resets preview to `null`.
- Error path captures error message.

---

### F5 — Newsletter status label pipe

**Location:** `libs/chairly/src/lib/newsletters/pipes/newsletter-status-label/newsletter-status-label.pipe.ts`

Maps status strings to Dutch labels:
- `Draft` → "Concept"
- `Scheduled` → "Ingepland"
- `Sending` → "Wordt verzonden"
- `Sent` → "Verzonden"
- `Cancelled` → "Geannuleerd"

Standalone, pure. Vitest tests cover all five values plus the unknown fallback.

---

### F6 — Add `ngx-quill` and configure rich text editor

- Install `ngx-quill` and `quill` packages in the Nx workspace (`src/frontend/chairly`).
- Add Quill snow theme stylesheet to the `apps/chairly/project.json` build styles list (after `tailwind.css` and `styles.scss`).
- Configure Quill toolbar with: bold, italic, underline, link, ordered/bulleted lists, headings (H2/H3), clean.
- Wrap usage in a small presentational component `libs/chairly/src/lib/newsletters/ui/newsletter-editor/newsletter-editor.component.ts` (`chairly-newsletter-editor`) with:
  - `value = model<string>('')` (two-way binding)
  - `placeholder = input<string>('Schrijf hier uw nieuwsbrief...')`
  - OnPush, standalone, templateUrl only.
  - Imports `QuillModule` (forRoot configured globally in `app.config.ts`).
- Add `QuillModule.forRoot({ ... })` to `apps/chairly/src/app/app.config.ts`'s providers array.

---

### F7 — Newsletter list page (smart component)

**Location:** `libs/chairly/src/lib/newsletters/feature/newsletter-list-page/`

`NewsletterListPageComponent` (`chairly-newsletter-list-page`, OnPush, standalone).

- Injects `NewsletterStore` and `Router`.
- On init: `store.loadCampaigns()`.
- Template:
  - Page header "Nieuwsbrief" with description "Stuur een marketing-e-mail naar al uw geabonneerde klanten."
  - Primary action button "Nieuwe nieuwsbrief" → navigates to `/nieuwsbrief/nieuw`.
  - Loading indicator while loading.
  - Empty state: "Nog geen nieuwsbrieven verstuurd." with an inline call-to-action button.
  - Table/cards listing campaigns: Subject, Status (using `NewsletterStatusLabelPipe` and a colored badge), Recipient count, Sent date or scheduled date, Created date.
  - Each row clickable → navigates to `/nieuwsbrief/{id}`.
- All Dutch copy from the first keystroke.
- Dark mode classes paired on every light background.

---

### F8 — Newsletter compose/edit page (smart component) with native `<dialog>` send-confirmation

**Location:** `libs/chairly/src/lib/newsletters/feature/newsletter-edit-page/`

Single component `NewsletterEditPageComponent` (`chairly-newsletter-edit-page`) used for both create and edit.

- Route param `id?: string` — when missing, the page is in create mode; when present, loads the campaign via `store.loadCampaign(id)`.
- Reactive typed FormGroup:
  - `subject: FormControl<string>` — required, max 500
  - `bodyHtml: FormControl<string>` — required, validated to be non-empty after stripping HTML tags
- Template:
  - Header: "Nieuwe nieuwsbrief" or "Nieuwsbrief bewerken"
  - Back link "Terug naar overzicht" → `/nieuwsbrief`
  - Field "Onderwerp" — text input
  - Field "Bericht" — `<chairly-newsletter-editor [(value)]="bodyHtmlControl">` (F6)
  - Hint: "Voeg geen tracking-pixels of scripts toe — deze worden automatisch verwijderd."
  - Buttons:
    - "Opslaan als concept" (primary) — create or update
    - "Voorbeeld bekijken" (secondary) — calls `store.loadPreview` and opens preview modal F10
    - "Test-e-mail naar mijzelf" (secondary) — calls `store.testSendCampaign(id)`; disabled until the campaign is saved (in create mode)
    - "Inplannen..." (secondary) — opens the schedule dialog F11
    - "Nu verzenden" (danger/primary) — opens a confirmation dialog "Weet u zeker dat u deze nieuwsbrief naar X klanten wilt versturen?" then calls `store.sendCampaign(id)`. Recipient count from `selectedCampaign().eligibleSubscribers`. **The confirmation dialog must be implemented as a native `<dialog>` element opened via `showModal()`, following the CLAUDE.md "Native `<dialog>` Pattern": full-screen overlay (`fixed inset-0 m-0 w-screen h-screen max-w-none max-h-none flex items-center justify-center border-0 bg-black/50 p-4`), inner card `bg-white dark:bg-slate-800 rounded-lg shadow-xl w-full max-w-md`, body overflow locked via injected `DOCUMENT` (`document.body.style.overflow = 'hidden'` on open, `''` on close), and Escape-to-close.** Do **not** use `window.confirm()`, `alert()`, or any ad-hoc modal implementation.
  - Read-only mode if status is `Sent`, `Sending`, or `Cancelled` — disable form controls and hide mutating buttons; show a banner with the status.
- Success/error banners surface from store state.
- After successful create: navigate to `/nieuwsbrief/{newId}` so subsequent saves are updates.

---

### F9 — Newsletter detail page (smart component)

**Location:** `libs/chairly/src/lib/newsletters/feature/newsletter-detail-page/`

`NewsletterDetailPageComponent` (`chairly-newsletter-detail-page`) for sent or scheduled campaigns.

- Route: `/nieuwsbrief/{id}`.
- Loads detail via store.
- Shows:
  - Subject and status badge
  - Sent timestamp / scheduled timestamp (formatted with Dutch locale)
  - Recipient counts: Totaal, Verzonden, Mislukt, In behandeling, Uitgeschreven
  - Rendered email preview (re-using the iframe pattern from F10 via the preview endpoint with the stored body)
  - Buttons: "Annuleren" for `Scheduled` campaigns, "Bewerken" for `Draft`, "Verwijderen" for `Draft`/`Scheduled`
  - Dutch confirmation dialogs for destructive actions

The list page (F7) routes to either the edit page (drafts) or the detail page (everything else) based on status.

---

### F10 — Newsletter preview modal (presentational component)

**Location:** `libs/chairly/src/lib/newsletters/ui/newsletter-preview-modal/`

`NewsletterPreviewModalComponent` (`chairly-newsletter-preview-modal`, OnPush, standalone).

- Inputs: `subject = input<string>('')`, `htmlBody = input<string>('')`.
- Native `<dialog>` overlay per CLAUDE.md pattern (`showModal()`, full-screen overlay, body overflow management via injected `DOCUMENT`).
- Uses `<iframe [attr.srcdoc]="htmlBody()">` (no `innerHTML`) at `max-w-2xl`.
- "Sluiten" button + Escape-to-close.
- Public `open()` / `close()` methods.

---

### F11 — Schedule newsletter dialog (presentational component)

**Location:** `libs/chairly/src/lib/newsletters/ui/schedule-newsletter-dialog/`

`ScheduleNewsletterDialogComponent` (`chairly-schedule-newsletter-dialog`, OnPush, standalone).

- Native `<dialog>` overlay per CLAUDE.md.
- Reactive form with a single date+time picker (reuse the existing shared date-time picker component) labeled "Verzenddatum en -tijd".
- Validation: must be at least 1 minute in the future. Dutch error message "Kies een tijdstip in de toekomst."
- Outputs: `confirmed = output<string>()` (ISO UTC string), `cancelled = output<void>()`.
- Buttons: "Bevestigen" / "Annuleren".

---

### F12 — Routes registration

**File:** `libs/chairly/src/lib/newsletters/newsletters.routes.ts`

```typescript
export const newslettersRoutes: Routes = [
  { path: '', loadComponent: () => import('./feature/newsletter-list-page/newsletter-list-page.component').then(m => m.NewsletterListPageComponent) },
  { path: 'nieuw', loadComponent: () => import('./feature/newsletter-edit-page/newsletter-edit-page.component').then(m => m.NewsletterEditPageComponent) },
  { path: ':id', loadComponent: () => import('./feature/newsletter-detail-page/newsletter-detail-page.component').then(m => m.NewsletterDetailPageComponent) },
  { path: ':id/bewerken', loadComponent: () => import('./feature/newsletter-edit-page/newsletter-edit-page.component').then(m => m.NewsletterEditPageComponent) },
];
```

**F12 is the sole owner of app-router wiring.** Register the lazy-loaded route in the top-level Angular app routing configuration (e.g. `apps/chairly/src/app/app.routes.ts`):

```typescript
{
  path: 'nieuwsbrief',
  loadChildren: () => import('@org/chairly-lib').then(m => m.newslettersRoutes),
}
```

Gate the route behind the Owner/Manager role guard used by other admin routes.

---

### F13 — Sidebar nav entry

Add a "Nieuwsbrief" item to the main sidebar nav configuration (visible only to Owner and Manager roles). Use a megaphone or envelope icon consistent with existing nav icons. Route target: `/nieuwsbrief`.

---

### F14 — Playwright e2e tests

**Location:** `apps/chairly-e2e/src/newsletter.spec.ts`

Scenarios (Dutch UI assertions, run as Owner unless stated):

1. Navigate to `/nieuwsbrief` — verify page heading "Nieuwsbrief" and the "Nieuwe nieuwsbrief" button.
2. Click "Nieuwe nieuwsbrief" — verify navigation to `/nieuwsbrief/nieuw` and that the editor renders.
3. Fill in subject "Lente-actie" and add some content via the Quill editor; click "Opslaan als concept" — verify success and URL changes to `/nieuwsbrief/{id}`.
4. Click "Voorbeeld bekijken" — verify preview modal opens with iframe; close via Escape key.
5. Click "Test-e-mail naar mijzelf" — verify success toast.
6. Click "Inplannen..." — pick a future date+time, confirm — verify status changes to "Ingepland".
7. Navigate back to list — verify campaign appears with "Ingepland" badge.
8. Open the scheduled campaign — click "Annuleren" — confirm — verify status flips to "Geannuleerd".
9. Create a second draft, click "Nu verzenden" — confirm dialog with recipient count — verify status becomes "Verzonden" or "Wordt verzonden".
10. Navigate to detail page — verify recipient counts and rendered preview iframe.
11. Sign in as Staff Member — verify `/nieuwsbrief` route is not in the sidebar and the URL returns/forbids access.
12. (Optional) Visit a known unsubscribe token URL and assert the Dutch confirmation page renders.

---

## Acceptance Criteria

- [ ] `Client.IsSubscribedToNewsletter` flag exists, defaults to `true`, migration is idempotent.
- [ ] `NewsletterCampaign` and `NewsletterDelivery` entities exist with timestamp pairs and no status enum column.
- [ ] EF migration `AddNewsletters` is fully idempotent.
- [ ] HTML sanitiser strips scripts, inline event handlers, and `javascript:` URLs.
- [ ] `INewsletterEventPublisher` and RabbitMQ implementation registered; tests use a Null/Recording double.
- [ ] CRUD endpoints for campaigns work for Owner and Manager only (Staff returns 403).
- [ ] Send endpoint materialises only subscribed clients with non-empty email and creates one delivery row per recipient with a unique unsubscribe token.
- [ ] Send endpoint publishes `NewsletterCampaignQueued` exactly once via the publisher (no direct `IEmailSender` calls in any handler (test-send uses `INewsletterEventPublisher`)).
- [ ] Scheduler hosted service queues due campaigns within 60 seconds of `ScheduledAtUtc`.
- [ ] Send worker marks deliveries as sent or failed and finalises `SentAtUtc` on the campaign.
- [ ] Preview endpoint returns sanitised HTML wrapped in the email layout including the salon name.
- [ ] Test-send endpoint sends a single email to the authenticated user's claim email.
- [ ] Public unsubscribe endpoint is anonymous, idempotent, sets `UnsubscribedAtUtc` on the delivery, and flips `Client.IsSubscribedToNewsletter` to `false`.
- [ ] Frontend `newsletters` domain library exists with Sheriff rules.
- [ ] Sidebar nav has a "Nieuwsbrief" entry visible to Owner and Manager only.
- [ ] List page, edit/compose page, detail page, preview modal, schedule dialog all use OnPush, signals, templateUrl only, Dutch copy.
- [ ] Rich text editor (`ngx-quill`) integrated and styled in dark mode.
- [ ] NgRx SignalStore handles all newsletter state with Vitest tests.
- [ ] Status label pipe localised to Dutch with Vitest tests.
- [ ] All forms use dropdowns/pickers (no raw IDs).
- [ ] Playwright e2e covers create → save → preview → schedule → cancel → send → detail flows.
- [ ] All backend quality checks pass (`dotnet build`, `dotnet test`, `dotnet format`).
- [ ] All frontend quality checks pass (`nx affected -t lint test build`, `nx format:check`).
- [ ] Playwright e2e tests pass.

## Out of Scope

- Audience segmentation / per-tag filtering (only `AllSubscribed` filter in MVP).
- A/B subject testing.
- Open/click tracking, analytics dashboards.
- Recurring or automated drip campaigns.
- Drag-and-drop / block-based template builder (Quill rich text only).
- Per-recipient personalisation tokens beyond salon name (no `{firstName}` etc. in MVP).
- Multi-language newsletter content.
- Re-subscribe flow from the unsubscribe page (one-click unsubscribe only).
- Bounced-email handling beyond storing `FailedAtUtc`/`FailureReason`.
- Importing external mailing lists.
- Attachments.
