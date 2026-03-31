# Booking Widget

## Overview

An embeddable booking widget that salon owners can place on their own website. Clients can browse services, pick a staff member (or choose "any available"), select a time slot, and create a booking -- all without logging in. The widget is served as a standalone Angular application loaded via an iframe. A new `PublicBooking` feature context provides unauthenticated API endpoints that resolve tenants by slug, return only public-safe data, and create bookings with automatic client matching by email.

## Domain Context

- Bounded context: **Bookings** (public-facing subset), **Onboarding/Subscriptions** (slug field)
- Key entities involved: **Subscription** (slug), **TenantSettings** (auto-confirm flag), **Service**, **ServiceCategory**, **StaffMember**, **Booking**, **BookingService**, **Client**
- Ubiquitous language: Booking (never "appointment"), Client (never "customer"), Staff Member (never "employee"), Service, Service Category, Tenant, Slug

## Backend Tasks

### B1 — Add Slug field to Subscription entity

Add a `Slug` property to the `Subscription` entity in `Chairly.Domain/Entities/Subscription.cs`. The slug is auto-generated from `SalonName` during subscription creation and is immutable.

**Entity change** (`Subscription`):

| Field | Type | Notes |
|---|---|---|
| `Slug` | `string` | required, max 100, unique, lowercase, hyphens only, auto-generated from SalonName |

**Slug generation rules:**
- Lowercase the salon name
- Replace spaces and special characters with hyphens
- Strip consecutive hyphens
- Strip leading/trailing hyphens
- Max 100 characters
- Create a utility method `SlugGenerator.Generate(string salonName)` in `Chairly.Api/Shared/Util/`

**EF Core configuration change** (`SubscriptionConfiguration`):
- Add `Slug` column: `required`, `MaxLength(100)`
- Add unique index on `Slug`

**Migration:**
- Create a new migration in `Chairly.Infrastructure/Migrations/Website/`
- Must be idempotent (use `DO $$ BEGIN IF NOT EXISTS ... END $$;` for AddColumn, `CREATE INDEX IF NOT EXISTS` for index)
- Backfill existing subscriptions: generate slug from SalonName for any rows where Slug is null

**CreateSubscription handler change:**
- Generate slug from `command.SalonName` using `SlugGenerator.Generate()`
- Check uniqueness in the database; if a collision occurs, append a numeric suffix (e.g. `salon-name-2`)
- Store the slug on the new subscription entity

**SubscriptionResponse change:**
- Add `Slug` property to the response record

**Test cases:**
- `SlugGenerator.Generate` produces correct slugs for various inputs (spaces, special chars, accents, consecutive hyphens)
- Slug uniqueness: when a duplicate slug exists, a numeric suffix is appended
- CreateSubscription handler sets the slug correctly
- SubscriptionResponse includes the slug

---

### B2 — Add AutoConfirmPublicBookings to TenantSettings

Add a boolean field to `TenantSettings` that controls whether public bookings are auto-confirmed or require manual confirmation.

**Entity change** (`TenantSettings`):

| Field | Type | Notes |
|---|---|---|
| `AutoConfirmPublicBookings` | `bool` | required, default `false` |

**EF Core configuration change** (`TenantSettingsConfiguration`):
- Add column with `.HasDefaultValue(false)`

**Migration:**
- Idempotent `AddColumn` with default value `false`

**Update TenantSettings endpoint:** `PUT /api/tenant-settings`
- Add `AutoConfirmPublicBookings` to the existing `UpdateTenantSettingsCommand` and `TenantSettingsResponse`
- Validation: boolean field, no additional validation needed beyond type binding
- Error codes:
  - 200 OK — settings updated successfully
  - 404 Not Found — tenant settings not found for the current tenant
  - 422 Unprocessable Entity — invalid request body

**Test cases:**
- Default value is `false` for new tenants
- Update handler correctly persists the flag
- Response includes the field
- Returns 404 when tenant settings not found

---

### B3 — Public booking endpoint: GET services

Create a new feature context `Chairly.Api/Features/PublicBooking/` with the first endpoint for fetching services.

**Slug resolution — 404 policy:** Return 404 for any slug that does not resolve to an active, provisioned subscription. This applies uniformly to slugs that are not found, belong to a subscription that is not yet provisioned, or belong to a cancelled subscription. No distinction is made between these cases to avoid information leakage about which slugs exist.

**Slice:** `Chairly.Api/Features/PublicBooking/GetPublicServices/`

**Endpoint:** `GET /api/public/book/{slug}/services`
- No authentication required (`.AllowAnonymous()`)
- Resolves `slug` to a `Subscription` via `WebsiteDbContext`, then uses the subscription's tenant ID to query the tenant database
- Returns only active services grouped by category

**Tenant resolution helper:**
- Create `Chairly.Api/Features/PublicBooking/Shared/SlugTenantResolver.cs`
- Method: `Task<Guid?> ResolveTenantIdAsync(string slug, CancellationToken ct)`
- Queries `WebsiteDbContext.Subscriptions` for a subscription with the given slug where `ProvisionedAtUtc` is set and `CancelledAtUtc` is null
- Returns the tenant ID (from the provisioned tenant) or null
- Returns null for any non-active case: slug not found, subscription not provisioned, subscription cancelled

**Rate limiting registration:**
- Register the built-in `Microsoft.AspNetCore.RateLimiting` middleware in `Program.cs` via `builder.Services.AddRateLimiter()`
- Define a named policy `"public-booking"` with a fixed window limiter: max 5 requests per IP per 15 minutes
- Apply the policy to the public booking endpoint group via `.RequireRateLimiting("public-booking")`

**Query:** `GetPublicServicesQuery` with `[Required] string Slug`

**Handler:** `GetPublicServicesHandler`
- Resolve tenant ID from slug (return 404 if not found)
- Query `ChairlyDbContext.Services` where `TenantId` matches and `IsActive == true`
- Include `Category` navigation property
- Group by category (null category = uncategorized group)
- Order categories by `SortOrder`, services within each category by `SortOrder`

**Response shape:**

```csharp
record PublicServiceCategoryResponse(
    Guid? CategoryId,
    string? CategoryName,
    PublicServiceResponse[] Services);

record PublicServiceResponse(
    Guid Id,
    string Name,
    string? Description,
    int DurationMinutes,
    decimal Price);
```

Note: `VatRate`, `SortOrder`, `CreatedAtUtc`, etc. are NOT exposed.

**Endpoint registration:**
- Create `PublicBookingEndpoints.cs` with `MapPublicBookingEndpoints()`
- Route group: `/api/public/book/{slug}` with `.AllowAnonymous()` and `.RequireRateLimiting("public-booking")`
- Register in `Program.cs`

**Test cases:**
- Returns 404 when slug does not exist
- Returns 404 when subscription is not provisioned
- Returns 404 when subscription is cancelled
- Returns grouped services sorted by category sort order
- Uncategorized services appear in a group with null category
- Inactive services are excluded
- Response does not include sensitive fields (VatRate, timestamps, etc.)

---

### B4 — Public booking endpoint: GET staff

**Slice:** `Chairly.Api/Features/PublicBooking/GetPublicStaff/`

**Endpoint:** `GET /api/public/book/{slug}/staff`
- No authentication required
- Returns only active staff members with minimal public data

**Query:** `GetPublicStaffQuery` with `[Required] string Slug`

**Handler:** `GetPublicStaffHandler`
- Resolve tenant ID from slug using `SlugTenantResolver` (return 404 if not found)
- Query `ChairlyDbContext.StaffMembers` where `TenantId` matches and `IsActive == true`
- Return only public-safe fields

**Response shape:**

```csharp
record PublicStaffResponse(
    Guid Id,
    string FirstName,
    string? PhotoUrl,
    string Color);
```

Note: `LastName`, `Email`, `PhoneNumber`, `Role`, and working hours details are NOT exposed.

**Test cases:**
- Returns 404 when slug does not exist
- Returns 404 when subscription is not provisioned
- Returns 404 when subscription is cancelled
- Returns only active staff members (where `IsActive == true`)
- Response contains only public-safe fields (no email, phone, role, working hours)
- Inactive staff members are excluded

---

### B5 — Public booking endpoint: GET availability

**Slice:** `Chairly.Api/Features/PublicBooking/GetPublicAvailability/`

**Endpoint:** `GET /api/public/book/{slug}/availability?date={date}&serviceIds={id1,id2}&staffMemberId={optional}`
- No authentication required
- Returns available time slots for a given date, set of services, and optional staff member

**Query:** `GetPublicAvailabilityQuery`

| Parameter | Type | Notes |
|---|---|---|
| `Slug` | `string` | from route, required |
| `Date` | `DateOnly` | from query string, required |
| `ServiceIds` | `List<Guid>` | from query string, required, min 1 |
| `StaffMemberId` | `Guid?` | from query string, optional (null = any available) |

**Handler:** `GetPublicAvailabilityHandler`
- Resolve tenant ID from slug using `SlugTenantResolver` (return 404 if not found)
- Load the requested services (return 404 if any service ID is invalid or inactive)
- Calculate total duration from the sum of service durations
- If `StaffMemberId` is provided, calculate slots for that staff member only; otherwise, calculate for all active staff members (where `IsActive == true`)
- For each staff member:
  1. Read `WorkingHours` (a `List<WorkingHoursEntry>`) and find entries matching the requested `DayOfWeek`. Each `WorkingHoursEntry` has `DayOfWeek` (DayOfWeek enum), `StartTime` (TimeOnly), and `EndTime` (TimeOnly).
  2. Generate candidate time slots at 15-minute intervals within working hours
  3. Exclude slots that overlap with existing bookings (non-cancelled, non-noshow) for that staff member
  4. Exclude slots where the booking end time (start + total duration) would exceed working hours
  5. Exclude slots in the past (for today's date)
- Return slots grouped by staff member

**Response shape:**

```csharp
record PublicAvailabilityResponse(
    PublicStaffAvailabilityResponse[] Staff);

record PublicStaffAvailabilityResponse(
    Guid StaffMemberId,
    string StaffFirstName,
    string Color,
    TimeSlotResponse[] Slots);

record TimeSlotResponse(
    TimeOnly StartTime,
    TimeOnly EndTime);
```

**Test cases:**
- Returns 404 when slug does not exist
- Returns 404 when subscription is not provisioned
- Returns 404 when subscription is cancelled
- Returns 404 when service IDs are invalid
- Returns empty slots when no staff works on the requested day
- Returns correct slots excluding existing bookings
- Slots do not exceed working hours
- Past time slots for today are excluded
- "Any staff" returns slots for all active staff members (where `IsActive == true`)
- Specific staff member returns only their slots
- 15-minute interval granularity

---

### B6 — Public booking endpoint: POST create booking

**Slice:** `Chairly.Api/Features/PublicBooking/CreatePublicBooking/`

**Endpoint:** `POST /api/public/book/{slug}/bookings`
- No authentication required
- Creates a booking for a walk-in client
- Rate limiting applied via the `"public-booking"` named policy (registered in B3): fixed window of max 5 requests per IP per 15 minutes; returns 429 Too Many Requests when exceeded

**Command:** `CreatePublicBookingCommand`

| Field | Type | Notes |
|---|---|---|
| `Slug` | `string` | from route, required |
| `StaffMemberId` | `Guid` | required (frontend resolves "any available" to a specific staff member) |
| `StartTime` | `DateTimeOffset` | required |
| `ServiceIds` | `List<Guid>` | required, min 1 |
| `ClientFirstName` | `string` | required, max 100 |
| `ClientLastName` | `string` | required, max 100 |
| `ClientEmail` | `string` | required, max 200, valid email format |
| `ClientPhoneNumber` | `string?` | optional, max 20 |
| `Notes` | `string?` | optional, max 1000 |
| `Honeypot` | `string?` | optional -- if non-empty, silently reject (return 201 with fake ID) |

**Handler:** `CreatePublicBookingHandler`

1. **Honeypot check:** If `Honeypot` is not null/empty, return a fake success response (201 with a random GUID) to avoid revealing the anti-spam mechanism
2. **Resolve tenant:** Look up slug in `WebsiteDbContext` (return 404 if not found/not provisioned/cancelled)
3. **Validate services:** Load active services by IDs from tenant database (return 404 if any missing/inactive)
4. **Validate staff:** Verify staff member exists and `IsActive == true` (return 404 if not found or inactive)
5. **Overlap check:** Verify the time slot is still available for the staff member (return 409 Conflict if overlapping)
6. **Client matching:** Search `ChairlyDbContext.Clients` for a client with matching email (case-insensitive) within the tenant
   - If found: use existing client ID, update name/phone if different
   - If not found: create a new `Client` entity
7. **Create booking:**
   - Build `Booking` entity with `CreatedBy = Guid.Empty` (no authenticated user)
   - Build `BookingService` entities with snapshotted service data
   - Calculate `EndTime` from `StartTime` + sum of service durations
8. **Auto-confirm:** Load `TenantSettings`; if `AutoConfirmPublicBookings == true`, set `ConfirmedAtUtc = DateTimeOffset.UtcNow` and `ConfirmedBy = Guid.Empty`
9. **Save and publish event:** Persist to database, then publish `BookingCreatedEvent` with an `IsPublic = true` flag. **Note:** The `BookingCreatedEvent` class (in `Chairly.Domain/Events/` or the relevant Bookings event publisher) must be updated to include a `bool IsPublic` property. If the event is published via `IBookingEventPublisher`, add the `IsPublic` parameter to the publish method signature. See B7 for notification details.

**Response shape:**

```csharp
record PublicBookingConfirmationResponse(
    Guid BookingId,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    string StaffFirstName,
    string[] ServiceNames,
    bool IsConfirmed);
```

**Test cases:**
- Returns 404 when slug does not exist
- Returns 404 when service or staff member is invalid
- Returns 409 when time slot has overlap
- Honeypot field: non-empty value returns fake 201, no booking is created
- Creates new client when email not found
- Reuses existing client when email matches (case-insensitive)
- Updates client name/phone when existing client has different values
- Booking is auto-confirmed when `AutoConfirmPublicBookings == true`
- Booking is in scheduled state when `AutoConfirmPublicBookings == false`
- BookingCreatedEvent is published with `IsPublic = true`
- Rate limiting returns 429 after 5 requests in 15 minutes
- Service data is snapshotted (name, duration, price)

---

### B7 — Notification integration for public bookings

Ensure the existing notification system handles public bookings. When a booking is created via the public widget:

1. **Client notification:** Send a booking confirmation email to the client's provided email address
2. **Salon notification:** Send a "new booking received" notification to the salon (using `CompanyEmail` from `TenantSettings`)

**Changes:**
- Reuse the existing `NotificationType.BookingReceived` enum value -- do NOT add a new enum value. The `BookingReceived` type already exists and is the correct type for salon-side "new booking" notifications.
- Add an `IsPublic` flag (or context property) to the `BookingCreatedEvent` so the event consumer can distinguish public vs dashboard bookings. This flag is used to select the appropriate email template wording (e.g. "via website" vs "via dashboard") but does NOT require a new `NotificationType`.
- The existing `BookingConfirmation` notification type handles the client-side email.
- Update the `BookingCreatedEvent` consumer (`BookingEventConsumer`) to check if the booking was created by a public user (`IsPublic == true` on the event) and, if so:
  - Send a `BookingConfirmation` notification to the client's email
  - Send a `BookingReceived` notification to the salon's `CompanyEmail`
- The email template for `BookingReceived` can include a conditional line like "via uw website" when the booking is public, but this is a template concern, not a new notification type.

**Test cases:**
- Public booking triggers client confirmation email (`BookingConfirmation`)
- Public booking triggers salon notification email (`BookingReceived`)
- Non-public booking does not trigger public-specific notification path
- `IsPublic` flag is correctly passed through the event

---

### B8 — Unit and integration tests for PublicBooking feature

Comprehensive tests for all public booking endpoints.

**Unit tests** (`Chairly.Tests/Features/PublicBooking/`):
- `SlugTenantResolverTests` — resolution with valid/invalid/cancelled subscriptions
- `GetPublicServicesHandlerTests` — service grouping, filtering, response shape
- `GetPublicStaffHandlerTests` — staff filtering (by `IsActive`), response shape
- `GetPublicAvailabilityHandlerTests` — slot calculation, overlap exclusion, working hours (`WorkingHours` / `WorkingHoursEntry`), past time filtering
- `CreatePublicBookingHandlerTests` — full booking flow, client matching, auto-confirm, honeypot, overlap

**Integration tests:**
- End-to-end API calls through the public endpoints
- Rate limiting behavior (429 after exceeding fixed window)
- Cross-database queries (WebsiteDbContext + ChairlyDbContext)

---

## Frontend Tasks

### F1 — Booking widget Angular application and library setup

Create a new Angular application and supporting library for the booking widget within the Nx monorepo.

**Application:** `apps/booking-widget/`
- Standalone Angular application (separate from the main `chairly` app)
- Minimal shell: no navigation, no auth, no sidebar
- Accepts the tenant slug from the URL path: `/book/{slug}`
- Configured for embedding in an iframe (no `X-Frame-Options` restriction on these assets)
- Tailwind CSS v4 for styling, matching the main app's setup pattern
- PostCSS config in JSON format for the Angular builder (`postcss.config.json`), plus `postcss.config.mjs` for Vite/Vitest tooling (both must be maintained per CLAUDE.md)
- **CRITICAL:** `apps/booking-widget/src/tailwind.css` must include `@custom-variant dark (&:where([data-theme=dark], [data-theme=dark] *));` -- without this, OS-level dark mode will partially activate `dark:` variants, causing unreadable text. Also include `@import 'tailwindcss'` and appropriate `@source` directives pointing at template files in both `apps/booking-widget/src/` and `libs/booking-widget/src/`.
- Dark mode support via `data-theme` attribute (can inherit from parent page via `postMessage` or default to light)

**Library:** `libs/booking-widget/`
- Create and register a new `libs/booking-widget/` Nx library in the workspace
- This library is **outside** the existing `libs/chairly/` and `libs/shared/` structure — it must NOT import from `libs/chairly/`
- Internal structure follows the standard DDD layers: `models/`, `data-access/`, `feature/{feature-name}/`, `ui/{component-name}/`

**Sheriff module boundary rules** — update `sheriff.config.ts`:
- Add tagging for `libs/booking-widget/src/lib` with domain layers (same pattern as other domains)
- Add a `booking-widget-lib` tag
- `apps/booking-widget/` can import from `libs/booking-widget/` and `libs/shared/` — it must NOT import from `libs/chairly/`
- `libs/booking-widget/` can import from `libs/shared/` — it must NOT import from `libs/chairly/` (domain isolation)

**E2E project:** `apps/booking-widget-e2e/`
- Scaffold a Playwright e2e project alongside the application (same pattern as `apps/chairly-e2e/`)
- Configure base URL to point at the booking widget dev server
- This e2e project is required in F1 so that F11 has a working project to write tests into

**Route structure:**
- `/book/:slug` — the booking widget root, loads the multi-step wizard

**App config** (`app.config.ts`):
- Register Dutch locale: call `registerLocaleData(localeNl)` and provide `{ provide: LOCALE_ID, useValue: 'nl-NL' }`
- Provide `{ provide: DEFAULT_CURRENCY_CODE, useValue: 'EUR' }` — `LOCALE_ID` alone does not change the currency default of `CurrencyPipe`
- Configure `HttpClient` with base API URL (configurable via environment)
- No auth interceptors (all endpoints are public)

**Test cases:**
- Application bootstraps without errors
- Route resolves the slug parameter

---

### F2 — Booking widget models and API service

Create TypeScript interfaces and API service for the public booking endpoints.

**Models** (`libs/booking-widget/src/lib/models/`):

```typescript
interface PublicServiceCategory {
  categoryId: string | null;
  categoryName: string | null;
  services: PublicService[];
}

interface PublicService {
  id: string;
  name: string;
  description: string | null;
  durationMinutes: number;
  price: number;
}

interface PublicStaffMember {
  id: string;
  firstName: string;
  photoUrl: string | null;
  color: string;
}

interface PublicAvailability {
  staff: PublicStaffAvailability[];
}

interface PublicStaffAvailability {
  staffMemberId: string;
  staffFirstName: string;
  color: string;
  slots: TimeSlot[];
}

interface TimeSlot {
  startTime: string; // HH:mm format
  endTime: string;
}

interface CreatePublicBookingRequest {
  staffMemberId: string;
  startTime: string; // ISO 8601
  serviceIds: string[];
  clientFirstName: string;
  clientLastName: string;
  clientEmail: string;
  clientPhoneNumber?: string;
  notes?: string;
  honeypot?: string;
}

interface PublicBookingConfirmation {
  bookingId: string;
  startTime: string;
  endTime: string;
  staffFirstName: string;
  serviceNames: string[];
  isConfirmed: boolean;
}
```

**API Service** (`libs/booking-widget/src/lib/data-access/booking-widget-api.service.ts`):
- `getServices(slug: string): Observable<PublicServiceCategory[]>`
- `getStaff(slug: string): Observable<PublicStaffMember[]>`
- `getAvailability(slug: string, date: string, serviceIds: string[], staffMemberId?: string): Observable<PublicAvailability>`
- `createBooking(slug: string, request: CreatePublicBookingRequest): Observable<PublicBookingConfirmation>`

**Test cases:**
- Service methods call the correct API endpoints
- Query parameters are correctly serialized for availability endpoint

---

### F3 — Booking widget NgRx SignalStore

Create a SignalStore to manage the multi-step booking wizard state.

**Store** (`libs/booking-widget/src/lib/data-access/booking-widget.store.ts`):

**State:**
- `slug: string`
- `currentStep: 'services' | 'staff' | 'datetime' | 'contact' | 'confirm'`
- `services: PublicServiceCategory[]`
- `staff: PublicStaffMember[]`
- `availability: PublicAvailability | null`
- `selectedServiceIds: string[]`
- `selectedStaffMemberId: string | null` (null = any available)
- `selectedDate: string | null` (ISO date)
- `selectedSlot: TimeSlot | null`
- `resolvedStaffMemberId: string | null` (the actual staff member for "any available")
- `clientFirstName: string`
- `clientLastName: string`
- `clientEmail: string`
- `clientPhoneNumber: string`
- `notes: string`
- `loading: boolean`
- `error: string | null`
- `confirmation: PublicBookingConfirmation | null`

**Computed signals:**
- `selectedServices` — the full service objects for selected IDs
- `totalDuration` — sum of selected service durations
- `totalPrice` — sum of selected service prices
- `canProceed` — whether the current step has valid selections to move forward
- `availableSlotsForSelectedStaff` — filtered slots based on selected staff member

**Methods:**
- `loadServices(slug: string)` — fetch and store services
- `loadStaff(slug: string)` — fetch and store staff
- `loadAvailability()` — fetch availability for selected date/services/staff
- `selectServices(ids: string[])` — update selected services
- `selectStaffMember(id: string | null)` — update selected staff (null = any)
- `selectDate(date: string)` — update date and trigger availability reload
- `selectSlot(slot: TimeSlot, staffMemberId: string)` — update selected slot
- `setContactInfo(...)` — update contact fields
- `goToStep(step)` — navigate to a wizard step
- `nextStep()` / `previousStep()` — sequential navigation
- `submitBooking()` — create the booking via API

**Test cases:**
- Step navigation works correctly
- Service selection updates computed totals
- Availability is reloaded when date or services change
- Submit sends correct payload

---

### F4 — Step 1: Service selection component

**Smart component:** `libs/booking-widget/src/lib/feature/service-step/`
- `service-step.component.ts` + `service-step.component.html`
- Injects the store, displays services grouped by category
- Each service is a selectable card showing name, description, duration ("X min"), and price (formatted as EUR with Dutch locale)
- Multiple services can be selected (toggle on/off)
- Shows a running total of selected services: "{N} diensten geselecteerd - {total duration} min - {total price}"
- "Volgende" (Next) button enabled when at least one service is selected

**Presentational component:** `libs/booking-widget/src/lib/ui/service-card/`
- `service-card.component.ts` + `service-card.component.html`
- Inputs: `service: PublicService`, `selected: boolean` (use `input()` signal API)
- Output: `toggled` event (use `OutputEmitterRef`)
- Visual: card with checkmark when selected, border highlight

**UI copy (Dutch):**
- Page title: "Kies je behandeling"
- Category headers: category name or "Overig" for uncategorized
- Duration: "{X} min"
- Price: formatted with `CurrencyPipe` (EUR, nl-NL locale)
- Footer: "{N} diensten geselecteerd"
- Button: "Volgende"

**Test cases:**
- Services are displayed grouped by category
- Selecting/deselecting services updates totals
- "Volgende" button is disabled when no services selected

---

### F5 — Step 2: Staff selection component

**Smart component:** `libs/booking-widget/src/lib/feature/staff-step/`
- `staff-step.component.ts` + `staff-step.component.html`
- Displays staff members as selectable cards
- First option is "Geen voorkeur" (No preference / any available) -- selected by default
- Each staff card shows first name, photo (or initials with color background), and color indicator

**Presentational component:** `libs/booking-widget/src/lib/ui/staff-card/`
- `staff-card.component.ts` + `staff-card.component.html`
- Inputs: `staffMember: PublicStaffMember | null` (null = "any" option), `selected: boolean` (use `input()` signal API)
- Output: `staffToggled` event (use `OutputEmitterRef`) -- named `staffToggled` (not `selected`) to avoid naming collision with the `selected` input

**UI copy (Dutch):**
- Page title: "Kies je medewerker"
- Any option: "Geen voorkeur"
- Any option subtitle: "Eerste beschikbare medewerker"
- Button: "Volgende"
- Back button: "Vorige"

**Test cases:**
- "Geen voorkeur" is selected by default
- Selecting a staff member updates the store
- Staff cards display name and photo/initials

---

### F6 — Step 3: Date and time selection component

**Smart component:** `libs/booking-widget/src/lib/feature/datetime-step/`
- `datetime-step.component.ts` + `datetime-step.component.html`
- Date picker (calendar) for selecting the date -- only future dates allowed
- When a date is selected, loads availability from the API
- Displays available time slots grouped by staff member (or for the selected staff member only)
- When "any available" was chosen in step 2, show all staff members' slots with their name and color

**Presentational components:**
- `libs/booking-widget/src/lib/ui/date-picker/` — calendar date selector
- `libs/booking-widget/src/lib/ui/time-slot-grid/` — grid of clickable time slot buttons

**UI copy (Dutch):**
- Page title: "Kies een datum en tijd"
- Date picker label: "Datum"
- No slots message: "Geen beschikbare tijden op deze datum"
- Loading: "Beschikbaarheid laden..."
- Button: "Volgende"
- Back button: "Vorige"

**Test cases:**
- Past dates are disabled in the calendar
- Selecting a date triggers availability load
- Time slots are displayed correctly
- Selecting a slot updates the store
- "No available slots" message is shown when appropriate

---

### F7 — Step 4: Contact information component

**Smart component:** `libs/booking-widget/src/lib/feature/contact-step/`
- `contact-step.component.ts` + `contact-step.component.html`
- Reactive form with typed FormGroup
- Fields: first name, last name, email, phone (optional), notes (optional)
- Hidden honeypot field (visually hidden with CSS, not `display:none` -- accessible to bots but not users)
- Validation: first name, last name, and email are required; email must be valid format

**UI copy (Dutch):**
- Page title: "Jouw gegevens"
- First name label: "Voornaam"
- Last name label: "Achternaam"
- Email label: "E-mailadres"
- Phone label: "Telefoonnummer (optioneel)"
- Notes label: "Opmerkingen (optioneel)"
- Email validation: "Vul een geldig e-mailadres in"
- Required validation: "Dit veld is verplicht"
- Button: "Bevestigen"
- Back button: "Vorige"

**Test cases:**
- Form validation prevents proceeding without required fields
- Email validation works
- Honeypot field is present in DOM but visually hidden

---

### F8 — Step 5: Confirmation component

**Smart component:** `libs/booking-widget/src/lib/feature/confirm-step/`
- `confirm-step.component.ts` + `confirm-step.component.html`
- Shows a summary of the booking before final submission
- Displays: selected services, staff member, date, time, contact info
- "Boeking plaatsen" (Place booking) button triggers submission
- After successful submission, shows a confirmation/thank-you screen
- After failed submission (409 conflict), shows error message and allows going back to date/time step

**UI copy (Dutch):**
- Page title: "Overzicht"
- Section headers: "Behandelingen", "Medewerker", "Datum & tijd", "Contactgegevens"
- Submit button: "Boeking plaatsen"
- Back button: "Vorige"
- Success title: "Boeking bevestigd!" (when auto-confirmed) or "Boeking ontvangen!" (when pending)
- Success message (confirmed): "Je boeking is bevestigd. Je ontvangt een bevestiging per e-mail."
- Success message (pending): "Je boeking is ontvangen en wordt zo snel mogelijk bevestigd. Je ontvangt een bevestiging per e-mail."
- Conflict error: "Dit tijdslot is helaas niet meer beschikbaar. Kies een ander tijdstip."
- General error: "Er is iets misgegaan. Probeer het opnieuw."

**Test cases:**
- Summary displays all selected options correctly
- Successful submission shows confirmation screen
- Conflict error navigates back to datetime step
- Confirmed vs pending booking shows different messages

---

### F9 — Wizard shell and progress indicator

**Smart component:** `libs/booking-widget/src/lib/feature/booking-wizard/`
- `booking-wizard.component.ts` + `booking-wizard.component.html`
- Container component that renders the current step
- Progress indicator showing all 5 steps with current step highlighted
- Step labels: "Behandeling", "Medewerker", "Datum & tijd", "Gegevens", "Bevestiging"
- Loads initial data (services, staff) on init using the slug from the route
- Handles loading and error states

**Presentational component:** `libs/booking-widget/src/lib/ui/step-indicator/`
- `step-indicator.component.ts` + `step-indicator.component.html`
- Inputs: `steps: string[]`, `currentStepIndex: number` (use `input()` signal API). The store tracks the current step as a string (`'services' | 'staff' | ...`); the wizard shell component converts this to a numeric index before passing it to `step-indicator`. Use the `steps` array's `indexOf()` for this conversion.
- Displays a horizontal progress bar with step dots/labels

**Salon branding:**
- Display the salon name in the widget header (fetched from the services/staff response or a separate lightweight endpoint)
- Use the salon's primary color for accents (future enhancement -- out of scope for now, use default Chairly colors)

**UI copy (Dutch):**
- Loading state: "Laden..."
- Error state: "Salon niet gevonden" (for 404) or "Er is iets misgegaan" (for other errors)
- Powered by: "Powered by Chairly" (shown in footer)

**Test cases:**
- Progress indicator shows correct current step
- Step transitions animate smoothly
- Loading state is shown while fetching initial data
- 404 error shows "Salon niet gevonden"

---

### F10 — Embed snippet and iframe host page

Create the embed infrastructure for salon owners.

**Embed snippet:**
- A simple HTML snippet that salon owners copy-paste into their website:
  ```html
  <iframe src="https://book.chairly.nl/{slug}" width="100%" height="700" frameborder="0"></iframe>
  ```
- The widget app serves at the `/book/{slug}` route

**Responsive iframe:**
- The widget should be responsive within its iframe container
- Minimum width: 320px (mobile)
- Maximum content width: 600px (centered within iframe)
- Height adjusts dynamically via `postMessage` to communicate content height to the parent page (stretch goal -- fixed height is acceptable for v1)

**Test cases:**
- Widget renders correctly at various widths (320px, 480px, 600px)
- Widget is functional within an iframe

---

### F11 — Playwright e2e tests for booking widget

Add end-to-end tests for the complete booking flow. Depends on F1 because the `apps/booking-widget-e2e/` Playwright project is scaffolded there.

**Test file:** `apps/booking-widget-e2e/src/booking-widget.spec.ts`

**Scenarios:**
1. **Full booking flow:** Select services -> Select staff -> Pick date/time -> Enter contact info -> Confirm -> See confirmation
2. **Invalid slug:** Navigate to non-existent slug, see "Salon niet gevonden"
3. **No preference staff:** Complete flow with "Geen voorkeur" selected
4. **Form validation:** Try to proceed without required contact fields, see validation messages
5. **Service selection:** Select and deselect services, verify totals update
6. **Date navigation:** Navigate between dates, verify slots change
7. **Back navigation:** Use "Vorige" buttons to navigate back through steps
8. **Responsive layout:** Test at mobile and desktop widths

---

## Acceptance Criteria

- [ ] Subscription entity has a `Slug` field, auto-generated from SalonName
- [ ] TenantSettings has `AutoConfirmPublicBookings` boolean field
- [ ] All four public API endpoints work without authentication
- [ ] Public endpoints only expose non-sensitive data (no emails, phones, roles, working hours)
- [ ] Services are grouped by category and sorted correctly
- [ ] Availability calculation correctly excludes overlapping bookings and respects working hours (using `WorkingHours` / `WorkingHoursEntry` with `DayOfWeek`, `StartTime`, `EndTime`)
- [ ] Inactive staff members are excluded using `IsActive == true` (not a timestamp column)
- [ ] Client matching by email works (create new or reuse existing)
- [ ] Auto-confirm flag is respected when creating public bookings
- [ ] Honeypot field silently rejects bot submissions
- [ ] Rate limiting (5 per IP per 15 min) is enforced via `Microsoft.AspNetCore.RateLimiting` with named policy `"public-booking"` on the public booking endpoint group; returns 429 when exceeded
- [ ] `BookingCreatedEvent` is published with `IsPublic` flag; existing `NotificationType.BookingReceived` is reused (no new enum value)
- [ ] 404 is returned for all non-active slugs (not found, not provisioned, cancelled) with no distinction to prevent information leakage
- [ ] Booking widget renders as a multi-step wizard with 5 steps
- [ ] All UI text is in Dutch
- [ ] Widget is responsive (320px - 600px)
- [ ] Widget works correctly within an iframe
- [ ] `libs/booking-widget/` is registered in the Nx workspace with Sheriff module boundaries preventing imports from `libs/chairly/`
- [ ] `apps/booking-widget-e2e/` Playwright project is scaffolded in F1
- [ ] `tailwind.css` includes `@custom-variant dark (&:where([data-theme=dark], [data-theme=dark] *));` directive
- [ ] All backend quality checks pass (dotnet build, test, format)
- [ ] All frontend quality checks pass (lint, test, build)
- [ ] Playwright e2e tests pass

## Out of Scope

- Custom salon branding (colors, logo) in the widget -- future enhancement
- Dynamic iframe height via `postMessage` -- use fixed height for v1
- CAPTCHA integration -- can be added later if honeypot + rate limiting proves insufficient
- Online payment during booking -- bookings are pay-at-salon only
- Multi-language support in the widget -- Dutch only for now
- Recurring/repeat booking from the widget
- Staff availability exceptions (holidays, sick days) -- uses regular working hours only
- Service-staff assignment (limiting which staff can perform which services)
- Slug editing/customization by salon owners -- immutable for now
- Web component distribution -- iframe only for v1
