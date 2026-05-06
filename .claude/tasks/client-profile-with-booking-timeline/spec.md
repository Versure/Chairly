# Client Profile with Booking Timeline

## Overview

Transform the existing client detail page into a richer client profile with a chronological booking timeline. Today the page only surfaces "completed bookings without a recipe" plus a recipe history list and a separate invoice table. This feature replaces those scattered sections with a single, month-grouped timeline of every booking the client has ever had (regardless of status), inlining the recipe and invoice that belong to each booking. A profile header shows aggregate stats (total visits, last visit, lifetime spend, most-visited staff member, most-booked service, no-show count) so staff can read the client at a glance before their next visit. Lives entirely in the existing `Clients` bounded context.

## Domain Context

- Bounded context: **Clients** — adds a new read slice `GetClientTimeline` alongside the existing client/recipe slices in `src/backend/Chairly.Api/Features/Clients/`.
- Key entities involved (read-only, no schema changes for this feature):
  - `Client` (existing aggregate root) — profile fields. **Prerequisite: the `Client` entity must expose `DeletedAtUtc` (DateTimeOffset?) and `DeletedBy` (Guid?) for soft-delete filtering** (per ADR-009 timestamp-pair convention; documented in `docs/domain-model.md`). If these fields are not yet present on the C# entity / EF configuration when this slice is implemented, B1 must add them and produce a corresponding idempotent migration before wiring the handler. This is the only schema concession in an otherwise read-only feature.
  - `Booking` + `BookingService` (existing) — every booking for the client, with all derived statuses.
  - `Recipe` + `RecipeProduct` (existing) — optionally attached to a completed booking.
  - `Invoice` + `InvoiceLineItem` (existing, Billing context) — read-only join to surface the invoice attached to a booking.
  - `StaffMember`, `Service` (existing) — joined for display name lookups.
- **Cross-bounded-context read — intentional and accepted.** This slice lives in the **Clients** context but reads `db.Invoices` (owned by the **Billing** context) directly via the shared `DbContext`. CLAUDE.md/ADR-005 require slices across bounded contexts not to reference each other through code. We deliberately make a narrow read-only exception here for query composition: this slice executes **no writes**, raises **no events**, and calls **no handlers** in the Billing context. It only projects existing invoice rows into a compact response DTO so the timeline UI can show the client's invoice next to each booking in a single round-trip. This is consistent with the existing precedent set by `GetClientRecipes` (Clients) which similarly reads aggregate data into the Clients context. If Billing later moves to its own database, this slice will switch to a Billing-side `IBillingReadModel` query — but for now, with both contexts in the same tenant database, the direct read is the simpler and well-bounded choice. Treat this paragraph as the documented rationale required by CLAUDE.md.
- Ubiquitous language:
  - **Booking** (never "appointment") — the core unit on the timeline.
  - **Recipe** — products/techniques used during a completed booking.
  - **Invoice** — billing document created from a completed booking.
  - **Client Profile Stats** — aggregate counters derived from a client's complete booking history.
  - **Booking Status** — derived from timestamps (ADR-009): `Scheduled`, `Confirmed`, `InProgress`, `Completed`, `Cancelled`, `NoShow`.

### Access Control

- Endpoint requires the existing `RequireStaff` policy used by the rest of `ClientEndpoints` — Owner, Manager, and Staff Member can all read the timeline of any client (per Decision 9: all staff see all bookings).
- **Staff Member access is intentionally permitted here — user decision: staff need full client history to better service the client.** This is a deliberate exception to the general "Staff Member cannot view all bookings" rule in the domain model permissions table; the exception is scoped to the client timeline context only and is documented under the Clients bounded context in `docs/domain-model.md` ("Business Rules (Client Timeline Visibility)").
- Recipe edit/create still respects existing ownership rules (the recipe form already enforces this — no change in this spec).

### Business Rules

- The timeline includes **every** booking for the client regardless of status (Decision 2). No status is filtered out by the backend; the UI applies status chip filters client-side (Decision 8).
- The endpoint is read-only and returns a single payload `{ profile, stats, timeline: [{ booking, recipe?, invoice? }] }` (Decision 4).
- Stats are computed from the booking history at request time:
  - `totalVisits` — count of bookings with `CompletedAtUtc` set.
  - `lastVisitAtUtc` — `MAX(StartTime)` over completed bookings, or `null`.
  - `totalSpentAmount` — sum of `Invoices.TotalAmount` for non-voided invoices linked to the client (`PaidAtUtc IS NOT NULL OR SentAtUtc IS NOT NULL` is **not** used here — sum **all** invoices that are not voided, i.e. `VoidedAtUtc IS NULL`). **Draft invoices (where `SentAtUtc IS NULL` and `PaidAtUtc IS NULL`) are intentionally included so the stat reflects all value associated with the client, not just finalized revenue.** This is a product decision: the timeline header is a client-level "lifetime spend" indicator for staff context, not a revenue accounting figure. Voided invoices are still excluded because they represent invoices that were retracted entirely.
  - `mostVisitedStaffMember` — staff member with highest count of `Completed` bookings; `null` if no completed bookings.
  - `mostBookedService` — service (by `ServiceId`) with highest count across all completed bookings' `BookingServices`; `null` if no completed bookings.
  - `noShowCount` — count of bookings with `NoShowAtUtc` set.
- Timeline items are returned ordered by `Booking.StartTime DESC` (newest first); the frontend groups them by month (Decision 7).
- Booking card payload includes everything needed for the card display (Decision 5: date/time, status, services, staff, total price, duration, notes, recipe/invoice presence).

---

## Backend Tasks

### B1 — `GetClientTimeline` query, handler, and endpoint

Add a new vertical slice that returns the full client profile, stats, and timeline in one call.

**Slice location:** `src/backend/Chairly.Api/Features/Clients/GetClientTimeline/`

**Files:**
- `GetClientTimelineQuery.cs`
- `GetClientTimelineHandler.cs`
- `GetClientTimelineEndpoint.cs`

**Endpoint:** `GET /api/clients/{clientId:guid}/timeline`

- Registered via a new `MapGetClientTimeline` extension method called from `ClientEndpoints.MapClientEndpoints`.
- Inherits the existing group-level `RequireAuthorization("RequireStaff")` policy. **Staff Member access is intentionally permitted here — user decision: staff need full client history to better service the client.** No additional role check is added inside the handler; all three roles (Owner, Manager, Staff Member) receive the full timeline. See Access Control section above.
- Returns `200 OK` with `ClientTimelineResponse`, or `404 Not Found` (mirroring the `OneOf<..., NotFound>` pattern used by `GetClientRecipesEndpoint`).

**Query record:**

```csharp
internal sealed record GetClientTimelineQuery(Guid ClientId)
    : IRequest<OneOf<ClientTimelineResponse, NotFound>>;
```

**Handler logic (single transaction, scoped to `tenantContext.TenantId`):**

1. Verify the client exists and is not soft-deleted (`Clients.AnyAsync(c => c.Id == query.ClientId && c.TenantId == tenantId && c.DeletedAtUtc == null)`). Return `NotFound` otherwise. (Requires `DeletedAtUtc` on the `Client` entity — see Domain Context prerequisite. If the field is missing, add `DeletedAtUtc` + `DeletedBy` to the entity, the EF Core configuration, and produce an idempotent migration as described in CLAUDE.md before this query is wired.)
2. Load the `ClientResponse` shape (reuse `ClientResponse` record from the existing slice for the `profile` block, but expose it through the new wrapper).
3. Load all bookings for the client in tenant scope: `Bookings.Include(b => b.BookingServices).Where(b => b.ClientId == query.ClientId && b.TenantId == tenantId).OrderByDescending(b => b.StartTime).ToListAsync()`. (Bookings are stored permanently — no soft-delete on bookings.)
4. Load the `StaffMember` rows for the bookings in a single query keyed by `b.StaffMemberId` (`db.StaffMembers.Where(s => staffIds.Contains(s.Id) && s.TenantId == tenantId)`). Build a `Dictionary<Guid, string>` of `StaffMemberId -> "FirstName LastName"`.
5. Load all recipes for the client (`db.Recipes.Include(r => r.Products).Where(r => r.ClientId == query.ClientId && r.TenantId == tenantId).ToListAsync()`). Build a `Dictionary<Guid, ClientRecipeSummaryResponse>` keyed by `BookingId` reusing the existing `ClientRecipeSummaryResponse` (with the recipe's staff member name resolved against the same staff lookup).
6. Load all invoices for the client (`db.Invoices.Where(i => i.ClientId == query.ClientId && i.TenantId == tenantId).ToListAsync()`) — project to a new compact `ClientTimelineInvoiceResponse` (defined below), keyed by `BookingId` in a `Dictionary<Guid, ClientTimelineInvoiceResponse>`.
7. Build the `timeline` list: for each booking in descending `StartTime` order, project a `TimelineEntryResponse(BookingTimelineCardResponse Booking, ClientRecipeSummaryResponse? Recipe, ClientTimelineInvoiceResponse? Invoice)`. The `Recipe` and `Invoice` lookups come from the dictionaries built in steps 5 and 6.
8. Compute stats (see Domain Context > Business Rules) — pure in-memory aggregation over the loaded data plus a single `Services.Where(s => serviceIds.Contains(s.Id) && s.TenantId == tenantId).Select(s => new { s.Id, s.Name }).ToListAsync()` lookup to translate `ServiceId` to a name for `mostBookedService`. (Booking services already snapshot `ServiceName`, so prefer the snapshot from the most-recent matching `BookingService` rather than an extra DB call when one is sufficient.)
9. Return `ClientTimelineResponse(Profile, Stats, Timeline)`.

**Performance notes:**
- A maximum of 5 small DB roundtrips for a typical client. No N+1 access.
- The handler is read-only; no `SaveChangesAsync`.

**Response DTOs (in the slice folder, `internal sealed record` everywhere, `internal` modifier per existing convention):**

```csharp
internal sealed record ClientTimelineResponse(
    ClientResponse Profile,
    ClientTimelineStatsResponse Stats,
    IReadOnlyList<TimelineEntryResponse> Timeline);

internal sealed record ClientTimelineStatsResponse(
    int TotalVisits,
    DateTimeOffset? LastVisitAtUtc,
    decimal TotalSpentAmount,
    StaffMemberSummary? MostVisitedStaffMember,
    ServiceSummary? MostBookedService,
    int NoShowCount);

internal sealed record StaffMemberSummary(Guid Id, string FullName, int VisitCount);
internal sealed record ServiceSummary(Guid Id, string Name, int BookingCount);

internal sealed record TimelineEntryResponse(
    BookingTimelineCardResponse Booking,
    ClientRecipeSummaryResponse? Recipe,
    ClientTimelineInvoiceResponse? Invoice);

internal sealed record BookingTimelineCardResponse(
    Guid Id,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    int DurationMinutes,
    string Status,            // derived: Scheduled|Confirmed|InProgress|Completed|Cancelled|NoShow
    Guid StaffMemberId,
    string StaffMemberName,
    decimal TotalPrice,
    string? Notes,
    IReadOnlyList<BookingTimelineServiceResponse> Services,
    DateTimeOffset? ConfirmedAtUtc,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    DateTimeOffset? CancelledAtUtc,
    DateTimeOffset? NoShowAtUtc);

internal sealed record BookingTimelineServiceResponse(
    Guid ServiceId,
    string ServiceName,
    int DurationMinutes,
    decimal Price,
    int SortOrder);

internal sealed record ClientTimelineInvoiceResponse(
    Guid Id,
    string InvoiceNumber,
    DateOnly InvoiceDate,
    decimal TotalAmount,
    string Status,            // derived: Draft|Sent|Paid|Void
    DateTimeOffset? SentAtUtc,
    DateTimeOffset? PaidAtUtc,
    DateTimeOffset? VoidedAtUtc);
```

- `Status` strings reuse the existing derivation logic (mirror `BookingMapper.DeriveStatus(booking)` and `InvoiceMapper.DeriveStatus(invoice)`); call those helpers instead of duplicating.
- `DurationMinutes` for a booking = `(int)(b.EndTime - b.StartTime).TotalMinutes`.
- `DurationMinutes` for each `BookingTimelineServiceResponse` = `(int)bs.Duration.TotalMinutes` (the `BookingService` value object stores `Duration` as `TimeSpan`; we cast its `TotalMinutes` to `int` to keep the wire shape an integer count).
- `TotalPrice` = `b.BookingServices.Sum(bs => bs.Price)`.

**Tests** (`src/backend/Chairly.Tests/`):
- Unit test for `GetClientTimelineHandler`:
  - Returns `NotFound` when the client does not exist.
  - Returns `NotFound` when the client belongs to a different tenant.
  - Returns `NotFound` when the client has `DeletedAtUtc` set.
  - Returns an empty timeline + zeroed stats when the client has no bookings.
  - Computes `totalVisits`, `lastVisitAtUtc`, `noShowCount` correctly across mixed-status bookings.
  - Computes `totalSpentAmount` excluding voided invoices.
  - Computes `mostVisitedStaffMember` correctly when one staff has more completed bookings than another.
  - Returns `mostVisitedStaffMember = null` when the client has only scheduled/cancelled bookings.
  - Computes `mostBookedService` correctly across multiple completed bookings.
  - Inlines the matching recipe on the booking that has one and `null` on bookings without.
  - Inlines the matching invoice on the booking that has one and `null` otherwise.
  - Timeline is ordered by `StartTime DESC`.
- Integration test for the endpoint (mirrors the pattern used in `Tests/Features/Clients/GetClientRecipesEndpointTests.cs`):
  - Returns 200 with the wrapped payload for a known client.
  - Returns 404 for an unknown client id.
  - Authenticates with the staff-member role for a client whose history contains bookings performed by **other** staff members, and asserts that the response timeline includes those bookings (i.e. the staff member receives the full booking history, not just their own). This is the regression guard for the Decision 9 / domain-model exception described in the Access Control section.
  - Tenant isolation: a client in tenant A is not retrievable by a user in tenant B.

---

## Frontend Tasks

### F1 — Timeline models and API method

**Models — `src/frontend/chairly/libs/chairly/src/lib/clients/models/`:**

Create `client-timeline.models.ts`:

```typescript
export type BookingStatus =
  | 'Scheduled'
  | 'Confirmed'
  | 'InProgress'
  | 'Completed'
  | 'Cancelled'
  | 'NoShow';

export type InvoiceStatus = 'Draft' | 'Sent' | 'Paid' | 'Void';

export interface BookingTimelineService {
  serviceId: string;
  serviceName: string;
  durationMinutes: number;
  price: number;
  sortOrder: number;
}

export interface BookingTimelineCard {
  id: string;
  startTime: string;
  endTime: string;
  durationMinutes: number;
  status: BookingStatus;
  staffMemberId: string;
  staffMemberName: string;
  totalPrice: number;
  notes: string | null;
  services: BookingTimelineService[];
  confirmedAtUtc: string | null;
  startedAtUtc: string | null;
  completedAtUtc: string | null;
  cancelledAtUtc: string | null;
  noShowAtUtc: string | null;
}

export interface ClientTimelineInvoice {
  id: string;
  invoiceNumber: string;
  invoiceDate: string;
  totalAmount: number;
  status: InvoiceStatus;
  sentAtUtc: string | null;
  paidAtUtc: string | null;
  voidedAtUtc: string | null;
}

export interface StaffMemberSummary {
  id: string;
  fullName: string;
  visitCount: number;
}

export interface ServiceSummary {
  id: string;
  name: string;
  bookingCount: number;
}

export interface ClientTimelineStats {
  totalVisits: number;
  lastVisitAtUtc: string | null;
  totalSpentAmount: number;
  mostVisitedStaffMember: StaffMemberSummary | null;
  mostBookedService: ServiceSummary | null;
  noShowCount: number;
}

export interface ClientTimelineEntry {
  booking: BookingTimelineCard;
  recipe: ClientRecipeSummary | null;
  invoice: ClientTimelineInvoice | null;
}

export interface ClientTimeline {
  profile: ClientResponse;
  stats: ClientTimelineStats;
  timeline: ClientTimelineEntry[];
}
```

`ClientResponse` and `ClientRecipeSummary` are imported from existing siblings (`./client.models`, `./recipe.models`). Add the new file to the `models/index.ts` barrel.

**API service — `src/frontend/chairly/libs/chairly/src/lib/clients/data-access/client-api.service.ts`:**

Add a single method:

```typescript
getClientTimeline(clientId: string): Observable<ClientTimeline> {
  return this.http.get<ClientTimeline>(`${this.baseUrl}/clients/${clientId}/timeline`);
}
```

Do **not** remove the existing `getClientBookings` method in this task — the recipe form auto-open flow still uses it via `completedBookingsWithoutRecipe`. F2 will refactor `ClientDetailPageComponent` to fully replace those legacy paths and then it is safe to keep `getClientBookings` only if any other consumer (e.g. tests) still imports it; if no consumers remain after F2, delete it as part of F2. Run a project-wide reference search before deletion.

**Tests:**
- Vitest unit test for `client-api.service.spec.ts` — extend existing spec with a `getClientTimeline` case asserting GET to `/clients/{id}/timeline`.

---

### F2 — Refactor `ClientDetailPageComponent` to use the timeline payload

Enhance the existing `client-detail-page` component (Decision 6) so it consumes the new endpoint as its single data source. Keep the `?bookingId=...` query param flow that auto-opens the recipe form for that booking.

**Location:** `src/frontend/chairly/libs/chairly/src/lib/clients/feature/client-detail-page/`

**Component changes (`client-detail-page.component.ts`):**

- Replace the four parallel loaders (`loadClient`, `loadRecipes`, `loadBookings`, `loadInvoices`) with one call: `clientApi.getClientTimeline(clientId)` returning a `ClientTimeline`.
- New signals:
  - `timeline = signal<ClientTimeline | null>(null)`
  - `isLoadingTimeline = signal<boolean>(true)`
  - `error = signal<string | null>(null)`
  - `statusFilter = signal<BookingStatus | 'All'>('All')` — chip filter state (Decision 8).
- Computed signals:
  - `client = computed(() => this.timeline()?.profile ?? null)`
  - `stats = computed(() => this.timeline()?.stats ?? null)`
  - `entries = computed(() => this.timeline()?.timeline ?? [])`
  - `filteredEntries = computed(() => filterByStatus(this.entries(), this.statusFilter()))`
  - `entriesByMonth = computed<MonthGroup[]>(...)` — groups `filteredEntries` into `{ monthKey: 'YYYY-MM', label: 'mei 2026', entries: ClientTimelineEntry[] }[]` ordered newest-first. Use `Intl.DateTimeFormat('nl-NL', { month: 'long', year: 'numeric' })` for the label (capitalize the first letter only — utility lives in `clients/util/format-month-label.ts`). **Import `MonthGroup` from `clients/util/group-timeline-by-month` (Sheriff allows `feature/` → `util/` imports).** Do not redefine `MonthGroup` inline in the component file; the canonical export lives alongside the `groupByMonth` pure function.
  - `completedBookingsWithoutRecipe` — derived from the **unfiltered** `entries()` signal (not `filteredEntries`) so the existing `?bookingId=...` auto-open flow still works even when the active status chip excludes Completed bookings: `this.entries().filter(e => e.booking.completedAtUtc !== null && e.recipe === null)`. Using `filteredEntries` here would be a bug — if the user has selected the "Geannuleerd" or "Gepland" chip and arrives via a deep link with `?bookingId={completedBookingId}`, the auto-open lookup would fail to find the booking. The recipe form auto-open is independent of the visible chip filter, so this signal must always reflect the full timeline.
  - `statusCounts = computed<Record<BookingStatus | 'All', number>>(...)` — feeds the chip filter component its label counts. Derivation: starts from `entries()` (the **unfiltered** timeline) so the chip totals reflect the full booking history regardless of which chip is currently active. Walks the unfiltered list once and increments counters for the booking's status; `'All'` is the total length. The chip labels in the UI ("Gepland" combines `Scheduled`, `Confirmed`, `InProgress`) are summed inside the `chairly-timeline-status-filter` template using these per-status counts so the filter component stays a pure renderer. Example shape: `{ All: 14, Scheduled: 2, Confirmed: 1, InProgress: 0, Completed: 9, Cancelled: 1, NoShow: 1 }`.
- The recipe-form auto-open flow (`pendingBookingId` + `tryAutoOpenRecipeForm`) keeps its current behavior; only the data source changes.
- After saving a recipe (`onRecipeSaved`) or editing the client (`onClientSaved`), reload the timeline via a single `loadTimeline()` call.
- After updating a client, the response of `clientApi.update(...)` patches the local `timeline` signal's `profile` block (immutable update) instead of triggering a full reload — no extra request.
- Remove `clientBookings`, `clientRecipes`, `clientInvoices` signals and their loaders. Stop importing `InvoiceGenerationService` from `@org/shared-lib` in this component (its only use here was `getClientInvoices`).

**Util — `src/frontend/chairly/libs/chairly/src/lib/clients/util/`:**

- `format-month-label.ts` — pure function `formatMonthLabel(date: Date): string` returning `"Mei 2026"`-style Dutch labels.
- `filter-timeline-by-status.ts` — pure function `filterByStatus(entries: ClientTimelineEntry[], status: BookingStatus | 'All'): ClientTimelineEntry[]`.
- `group-timeline-by-month.ts` — pure function `groupByMonth(entries: ClientTimelineEntry[]): MonthGroup[]`. Define `MonthGroup` locally and export from the same file.
- Vitest tests for each util.

**Status filter chips (presentational subcomponent in `ui/`):**

Create `src/frontend/chairly/libs/chairly/src/lib/clients/ui/timeline-status-filter/`:

- Component selector: `chairly-timeline-status-filter`.
- API (signal-based per CLAUDE.md):
  - `value = model.required<BookingStatus | 'All'>()` — two-way bindable model; clicking a chip writes the new value via `this.value.set(...)`. **No separate `valueChange` output** — the implicit `valueChange` from `model()` covers the parent binding.
  - `counts = input.required<Record<BookingStatus | 'All', number>>()` — per-status counts plus the `'All'` total. The "Gepland" chip count is computed inside the template as `counts().Scheduled + counts().Confirmed + counts().InProgress`.
- Parent template binding uses two-way syntax: `<chairly-timeline-status-filter [(value)]="statusFilter" [counts]="statusCounts()" />`. (`statusFilter` is a writable `signal`, so it can be bound directly to a `model()`.)
- Renders five chips in this order with these Dutch labels (Decision 8): `Alle`, `Voltooid` (Completed), `Geannuleerd` (Cancelled), `No-show` (NoShow), `Gepland` (Scheduled+Confirmed+InProgress combined).
- Each chip shows the count next to the label (e.g. "Voltooid (12)").
- The selected chip uses the existing primary chip style; unselected chips use neutral.
- OnPush, standalone, templateUrl only.
- Vitest test asserts that clicking a chip updates the `value` model (emits via the implicit `valueChange`) and that the selected style is applied to the matching chip.

> Note: the "Gepland" chip filters bookings whose status is `Scheduled`, `Confirmed`, or `InProgress` (i.e. anything not yet a terminal state). Implement the mapping inside `filter-timeline-by-status.ts`.

**Booking timeline card (presentational subcomponent in `ui/`):**

Create `src/frontend/chairly/libs/chairly/src/lib/clients/ui/booking-timeline-card/`:

- Component selector: `chairly-booking-timeline-card`.
- Inputs: `entry = input.required<ClientTimelineEntry>()`, `canEditRecipe = input<boolean>(true)`.
- Outputs: `addRecipe = output<BookingTimelineCard>()`, `editRecipe = output<ClientRecipeSummary>()`.
- Card layout (Decision 5):
  - Top row: date (e.g. `do 14 mei 2026`), time range `13:30 – 14:15`, status badge (Dutch label via local map), duration pill `45 min`.
  - Staff: `Met {staffMemberName}`.
  - Services: bullet list of `{serviceName} — €{price}` per service.
  - Total price right-aligned: `€{totalPrice}` using `CurrencyPipe` with `'EUR'`.
  - Notes (if present): collapsible "Notities" toggle below the services.
  - Bottom action row:
    - If `entry.recipe` exists → text link "Recept bekijken / bewerken" emitting `editRecipe`.
    - Else if booking is `Completed` and `canEditRecipe` → primary-light button "Recept toevoegen" emitting `addRecipe(entry.booking)`.
    - If `entry.invoice` exists → text link "Factuur {invoiceNumber}" routing to `/facturen/{invoice.id}` (use `[routerLink]`). Show a status pill next to it ("Concept" / "Verzonden" / "Betaald" / "Vervallen").
- Status badge color mapping (Tailwind classes; pair every light variant with `dark:`):
  - `Scheduled` → blauw (`bg-blue-100 text-blue-800 dark:bg-blue-900/40 dark:text-blue-200`)
  - `Confirmed` → indigo
  - `InProgress` → amber
  - `Completed` → green
  - `Cancelled` → gray
  - `NoShow` → red
- Status Dutch labels:
  - `Scheduled` → "Gepland"
  - `Confirmed` → "Bevestigd"
  - `InProgress` → "Bezig"
  - `Completed` → "Voltooid"
  - `Cancelled` → "Geannuleerd"
  - `NoShow` → "No-show"
- The mapping lives in a new pipe `clients/pipes/booking-status-label/booking-status-label.pipe.ts` (standalone, pure, Vitest-tested) used by the card and the chip filter for consistency.
- OnPush, standalone, templateUrl only, signal-based APIs.

**Profile header (presentational subcomponent in `ui/`):**

Create `src/frontend/chairly/libs/chairly/src/lib/clients/ui/client-profile-header/`:

- Component selector: `chairly-client-profile-header`.
- Inputs: `client = input.required<ClientResponse>()`, `stats = input.required<ClientTimelineStats>()`.
- Output: `editClient = output<void>()`.
- Renders the client's name, email, phone, notes, and a stats row (Decision 3) with these Dutch labels:
  - "Bezoeken" — `stats.totalVisits`.
  - "Laatste bezoek" — formatted `stats.lastVisitAtUtc` with `DatePipe('d MMM yyyy', undefined, 'nl-NL')`, falling back to "—".
  - "Totale omzet" — `stats.totalSpentAmount | currency:'EUR':'symbol':'1.2-2':'nl-NL'`.
  - "Vaste medewerker" — `stats.mostVisitedStaffMember?.fullName ?? '—'` (with smaller secondary text "(N bezoeken)").
  - "Favoriete dienst" — `stats.mostBookedService?.name ?? '—'` (with secondary text "(N keer geboekt)").
  - "No-shows" — `stats.noShowCount`.
- Layout: 2-row 3-column grid on `md:` and up; single column on mobile.
- "Bewerken" button at the top-right emits `editClient` (replaces the existing Bewerken button from the page header).
- OnPush, standalone, templateUrl only.

**Template — `client-detail-page.component.html`:**

Replace the current sections with:

1. Page header (back link "← Terug naar klanten" + page title) — keep as today.
2. `<chairly-client-profile-header [client]="client()!" [stats]="stats()!" (editClient)="onEditClient()" />`
3. `<chairly-timeline-status-filter [(value)]="statusFilter" [counts]="statusCounts()" />` — uses the implicit `valueChange` from `model.required(...)` together with the `statusFilter` writable signal.
4. Loop over `entriesByMonth()`:
   - Each month group renders an `<h2>` with the month label and a thin separator.
   - Inside, loop over the group's entries and render `<chairly-booking-timeline-card [entry]="entry" (addRecipe)="onAddRecipe($event)" (editRecipe)="onEditRecipe($event)" />`.
5. Empty state when `filteredEntries().length === 0`:
   - If the underlying timeline is empty: "Deze klant heeft nog geen boekingen."
   - If only the active filter is empty: "Geen boekingen voor dit filter."
6. Loading indicator while `isLoadingTimeline()`.
7. Keep the `<chairly-client-form-dialog>` and `<chairly-recipe-form>` always-mounted dialogs at the bottom (unchanged).

**Dark mode:** every light background must have a paired `dark:` variant per CLAUDE.md.

**Vitest tests** (`*.spec.ts` next to each new file):
- `format-month-label.spec.ts` — capitalisation and locale.
- `filter-timeline-by-status.spec.ts` — each chip value, "Gepland" combines three statuses, "Alle" is identity.
- `group-timeline-by-month.spec.ts` — entries spanning multiple months are grouped and sorted desc.
- `booking-status-label.pipe.spec.ts` — every status string and unknown fallback.
- Component test for `ClientDetailPageComponent` (extends existing spec if present; otherwise create one): mocks `ClientApiService.getClientTimeline` and asserts that the page renders the profile header, the chip filter, and the month groups.

---

### F3 — Playwright e2e coverage

**Location:** `src/frontend/chairly/apps/chairly-e2e/src/client-profile-timeline.spec.ts`

Full e2e coverage (Decision 10) using the existing seed data and authenticated salon Owner login flow. Scenarios:

1. Sign in as Owner, navigate to `/klanten/{seededClientWithHistoryId}` — assert the page heading shows the client's name and the profile header card is visible.
2. Profile stats are populated: locate `text=Bezoeken` and assert the adjacent number is non-empty; assert `Totale omzet` shows a Euro currency value (`€` glyph present, Dutch decimal formatting).
3. Status filter chips are rendered with Dutch labels: `Alle`, `Voltooid`, `Geannuleerd`, `No-show`, `Gepland`. Selecting `Voltooid` re-renders the timeline and every visible status badge says `Voltooid`. Selecting `Alle` brings back all bookings.
4. Timeline entries are grouped by month — assert at least one `<h2>` with a Dutch month label (e.g. `mei 2026` or `januari 2026`).
5. A booking card shows: date, time range, status badge, staff name (with the prefix "Met "), service list, total price, duration pill (`min` suffix).
6. Clicking "Recept toevoegen" on a Completed booking without a recipe opens the recipe form dialog. Cancel via Escape (per CLAUDE.md `<dialog>` testing guidance) and assert it closes.
7. Clicking "Recept bekijken / bewerken" on a booking that has a recipe opens the recipe form pre-filled. Close via Escape.
8. A booking with an invoice shows a link "Factuur {nummer}" — clicking it navigates to `/facturen/{id}` (assert URL).
9. Clicking "Bewerken" in the profile header opens the client edit dialog and submitting saves the updates and the header reflects the new name without a full page reload.
10. The `?bookingId={id}` query param flow still auto-opens the recipe form for that booking — visit `/klanten/{id}?bookingId={completedBookingWithoutRecipe}` and assert the recipe form is open on load.
11. Empty state for a brand-new client: navigate to `/klanten/{seededEmptyClientId}` and assert the message "Deze klant heeft nog geen boekingen."
12. Filter empty state: pick a status that has no bookings for the test client and assert "Geen boekingen voor dit filter."

If a seed lacking the right data exists, add a deterministic seed extension in `Chairly.Infrastructure/Persistence/Seeding/` (`ClientWithHistorySeeder`) that creates one client with at least one Completed booking with a recipe + invoice, one Cancelled booking, one NoShow booking, and one Scheduled booking — guarded with the standard idempotency pattern used by other seeders. Reference this seeded client by a stable Guid in the test fixture file.

---

## Acceptance Criteria

- [ ] `GET /api/clients/{clientId}/timeline` returns `{ profile, stats, timeline }` with the documented DTO shapes.
- [ ] Endpoint is registered under the existing `RequireStaff` policy and uses the `OneOf<..., NotFound>` pattern.
- [ ] Tenant isolation: a client in tenant A is never returned to a user in tenant B.
- [ ] Stats are computed correctly: `totalVisits`, `lastVisitAtUtc`, `totalSpentAmount` (excludes voided invoices), `mostVisitedStaffMember`, `mostBookedService`, `noShowCount`.
- [ ] Timeline includes every booking regardless of status, ordered by `StartTime DESC`.
- [ ] Each timeline entry inlines the optional recipe and invoice for that booking.
- [ ] Backend handler unit tests cover all stat permutations and the 404 paths.
- [ ] Backend integration test asserts staff member can read the timeline.
- [ ] Frontend `ClientApiService.getClientTimeline` exists and is the single data source for the detail page.
- [ ] `ClientDetailPageComponent` shows a profile header (with stats), status filter chips, and a month-grouped booking timeline using only the new endpoint.
- [ ] Recipe form auto-open via `?bookingId=` query param still works.
- [ ] All Dutch UI copy is in place from the first keystroke (no English strings).
- [ ] Booking status filter chips: `Alle`, `Voltooid`, `Geannuleerd`, `No-show`, `Gepland`.
- [ ] Booking status badge colors match the documented mapping with paired `dark:` variants.
- [ ] No raw ID inputs introduced — entity selection (when needed) goes through dropdowns; the timeline uses no entity selection forms.
- [ ] Vitest unit tests cover all new utils, the new pipe, and the new components.
- [ ] Playwright e2e covers all twelve scenarios listed in F3.
- [ ] All backend quality checks pass (`dotnet build src/backend/Chairly.slnx`, `dotnet test src/backend/Chairly.slnx`, `dotnet format src/backend/Chairly.slnx --verify-no-changes`).
- [ ] All frontend quality checks pass (`npx nx affected -t lint`, `npx nx format:check`, `npx nx affected -t test`, `npx nx affected -t build`).
- [ ] Playwright e2e tests pass.

## Out of Scope

- Schema changes to `Booking`, `Recipe`, or `Invoice` — the only schema concession is adding `DeletedAtUtc`/`DeletedBy` to `Client` if not already present (required for soft-delete filtering).
- Editing booking status from the timeline (no inline state transitions; users keep using the bookings module for that).
- Editing invoices from the timeline (the link navigates to the existing invoice detail page).
- Pagination of the timeline — MVP returns the entire history in one payload. (Performance budget: a typical client has fewer than ~500 bookings; the response size is acceptable.)
- Per-staff-member timeline filtering on the same page (a staff filter chip is not in scope for this iteration).
- Deleting a client from the timeline page.
- Any change to the recipe permission model (Decision 9 explicitly preserves existing recipe ownership rules).
- Any change to the bookings module, the recipes module's standalone screens, or the invoices module.
- Charts, graphs, or year-over-year comparison stats.
- Exporting the timeline to PDF or CSV.
