# Dashboard

## Overview

The dashboard provides salon staff with a real-time overview of daily operations: today's bookings, upcoming appointments, revenue metrics, and new client counts. It serves as the landing page after login, replacing the current redirect to "diensten". The data is aggregated from the Bookings, Clients, and Billing bounded contexts into a single read-only endpoint, with role-based visibility: Owners see all stats including revenue, Managers see bookings and new clients, and Staff Members see only their own bookings.

## Domain Context

- Bounded context: **Dashboard** (cross-cutting read model, aggregates data from Bookings, Clients, Billing)
- Key entities involved: **Booking**, **BookingService**, **Client**, **Invoice**, **StaffMember**
- Ubiquitous language: Booking (never "appointment"), Client (never "customer"), Staff Member (never "employee")

## Backend Tasks

### B1 — Dashboard response records

Create the response records for the dashboard endpoint in `Chairly.Api/Features/Dashboard/`.

**DashboardBookingResponse** (`Chairly.Api/Features/Dashboard/DashboardBookingResponse.cs`):
```csharp
internal sealed record DashboardBookingResponse(
    Guid Id,
    Guid ClientId,
    string ClientName,
    Guid StaffMemberId,
    string StaffMemberName,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    string Status,
    IReadOnlyList<string> ServiceNames);
```

**DashboardResponse** (`Chairly.Api/Features/Dashboard/DashboardResponse.cs`):
```csharp
internal sealed record DashboardResponse(
    int TodaysBookingsCount,
    IReadOnlyList<DashboardBookingResponse> TodaysBookings,
    IReadOnlyList<DashboardBookingResponse> UpcomingBookings,
    int NewClientsThisWeek,
    decimal? RevenueThisWeek,
    decimal? RevenueThisMonth);
```

Notes:
- `RevenueThisWeek` and `RevenueThisMonth` are nullable — set to `null` for non-Owner roles
- `NewClientsThisWeek` is set to `0` for Staff Member role (they do not have access)
- `ClientName` and `StaffMemberName` are resolved via joins (not stored on booking)
- `ServiceNames` is a flat list of service names from the booking's `BookingServices`
- `Status` is derived from timestamps using the existing `BookingMapper.DeriveStatus` pattern from the Bookings feature (reuse `Booking.DeriveStatus()` extension method from Domain)

**Test cases:**
- No tests for this task (plain record types)

---

### B2 — GetDashboard query and handler

Create the `GET /api/dashboard` endpoint following the vertical slice pattern.

**Slice location:** `Chairly.Api/Features/Dashboard/GetDashboard/`

**GetDashboardQuery** (`GetDashboardQuery.cs`):
```csharp
internal sealed record GetDashboardQuery : IRequest<DashboardResponse>;
```

**GetDashboardHandler** (`GetDashboardHandler.cs`):

Constructor dependencies: `ChairlyDbContext db`, `ITenantContext tenantContext`

Handler logic (in order):

1. Determine the current user's role from `tenantContext.UserRole` (string: `"owner"`, `"manager"`, or `"staff_member"` -- follows the established `ITenantContext` string convention, see `TenantContextMiddleware._knownRoles`) and their `UserId` from `tenantContext.UserId`
2. Resolve the current user's `StaffMemberId` by querying `db.StaffMembers.Where(s => s.TenantId == tenantContext.TenantId && s.UserId == tenantContext.UserId)`. This is needed for Staff Member role filtering.
3. **Today's bookings:**
   - Query `db.Bookings` for `TenantId`, where `StartTime` falls within today (UTC, midnight to midnight)
   - Include `BookingServices`
   - Join with `Clients` (for `ClientName = FirstName + " " + LastName`) and `StaffMembers` (for `StaffMemberName = FirstName + " " + LastName`)
   - If role is `"staff_member"`, filter to `StaffMemberId == currentStaffMemberId`
   - Order by `StartTime` ascending
   - Map to `DashboardBookingResponse`
   - Count = `.Count()` of the filtered result
4. **Upcoming bookings (next 5 from now):**
   - Query `db.Bookings` for `TenantId`, where `StartTime > DateTimeOffset.UtcNow` and `CancelledAtUtc == null` and `NoShowAtUtc == null` and `CompletedAtUtc == null`
   - Include `BookingServices`
   - Join with `Clients` and `StaffMembers` for names
   - If role is `"staff_member"`, filter to `StaffMemberId == currentStaffMemberId`
   - Order by `StartTime` ascending, take 5
   - Map to `DashboardBookingResponse`
5. **New clients this week:**
   - If role is `"owner"` or `"manager"`: query `db.Clients` for `TenantId`, where `CreatedAtUtc` >= start of current ISO week (Monday), count
   - If role is `"staff_member"`: set to `0`
6. **Revenue this week (Owner only):**
   - If role is `"owner"`: query `db.Invoices` for `TenantId`, where `PaidAtUtc` is not null and `VoidedAtUtc` is null and `PaidAtUtc` >= start of current ISO week (Monday), sum `TotalAmount`
   - Otherwise: `null`
7. **Revenue this month (Owner only):**
   - If role is `"owner"`: query `db.Invoices` for `TenantId`, where `PaidAtUtc` is not null and `VoidedAtUtc` is null and `PaidAtUtc` >= first day of current month, sum `TotalAmount`
   - Otherwise: `null`
8. Return `DashboardResponse` with all aggregated data

**Date calculation helpers:**
- Today range: `DateTimeOffset todayStart = new DateTimeOffset(DateTimeOffset.UtcNow.Date, TimeSpan.Zero)` (midnight UTC), `DateTimeOffset todayEnd = todayStart.AddDays(1)`
- Week start: ISO 8601 Monday. Use `DayOfWeek` calculation: `DateTime today = DateTimeOffset.UtcNow.Date; int diff = (7 + (int)today.DayOfWeek - (int)DayOfWeek.Monday) % 7; DateTimeOffset weekStart = new DateTimeOffset(today.AddDays(-diff), TimeSpan.Zero);`
- Month start: `new DateTimeOffset(new DateTime(DateTimeOffset.UtcNow.Year, DateTimeOffset.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc), TimeSpan.Zero)`

**Test cases:**
- Returns today's bookings count and list for all roles
- Staff Member sees only their own bookings in today's list
- Owner/Manager sees all bookings in today's list
- Upcoming bookings returns max 5, ordered by StartTime ascending
- Upcoming bookings excludes cancelled, no-show, and completed bookings
- Staff Member sees only their own upcoming bookings
- New clients this week returns correct count for Owner
- New clients this week returns correct count for Manager
- New clients this week returns 0 for Staff Member
- Revenue this week returns sum of paid (non-voided) invoice totals for Owner
- Revenue this week returns null for Manager
- Revenue this week returns null for Staff Member
- Revenue this month returns sum of paid (non-voided) invoice totals for Owner
- Revenue this month returns null for non-Owner roles
- Revenue sums are 0 (not null) for Owner when no invoices exist in the period

---

### B3 — Dashboard endpoint registration

Create the endpoint and wire it into `Program.cs`.

**GetDashboardEndpoint** (`Chairly.Api/Features/Dashboard/GetDashboard/GetDashboardEndpoint.cs`):
- Extension method `MapGetDashboard(this IEndpointRouteBuilder group)` on the group
- `GET /` on the dashboard group
- Sends `GetDashboardQuery` via mediator
- Returns `Results.Ok(response)`

**DashboardEndpoints** (`Chairly.Api/Features/Dashboard/DashboardEndpoints.cs`):
```csharp
internal static class DashboardEndpoints
{
    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/dashboard")
            .RequireAuthorization("RequireStaff");

        group.MapGetDashboard();

        return app;
    }
}
```

**Program.cs changes:**
- Add `app.MapDashboardEndpoints();` alongside existing endpoint registrations

**Test cases:**
- Integration test: `GET /api/dashboard` returns 200 with valid `DashboardResponse` shape
- Integration test: endpoint requires authentication (returns 401 without token)

---

### B4 — Unit tests for dashboard handler

Create unit tests in `Chairly.Tests/Features/Dashboard/GetDashboardHandlerTests.cs`.

Follow existing test patterns (see `BookingHandlerTests.cs`):
- Use `InMemoryDatabase` for DbContext
- Create helper methods for test data setup
- Each test method tests one scenario
- Use `[Fact]` attribute

Cover all test cases listed in B2. Group tests logically:

**Today's bookings:**
- Happy path: returns all today's bookings with client/staff names
- Staff Member role: filters to own bookings only
- Empty result: returns count 0 and empty list when no bookings today

**Upcoming bookings:**
- Returns next 5 future bookings ordered by StartTime
- Excludes cancelled, no-show, and completed bookings
- Staff Member role: filters to own bookings only
- Returns fewer than 5 if not enough upcoming bookings

**New clients this week:**
- Owner sees correct count
- Manager sees correct count
- Staff Member sees 0
- Counts only clients created since Monday

**Revenue:**
- Owner: correct sum for week and month
- Owner: returns 0m (not null) when no paid invoices
- Owner: excludes voided invoices
- Manager/Staff Member: returns null for both fields

## Frontend Tasks

### F1 — Dashboard models

Create TypeScript interfaces for the dashboard API response in `libs/chairly/src/lib/dashboard/models/`.

**File:** `dashboard.models.ts`

```typescript
export interface DashboardBooking {
  id: string;
  clientId: string;
  clientName: string;
  staffMemberId: string;
  staffMemberName: string;
  startTime: string;
  endTime: string;
  status: string;
  serviceNames: string[];
}

export interface DashboardResponse {
  todaysBookingsCount: number;
  todaysBookings: DashboardBooking[];
  upcomingBookings: DashboardBooking[];
  newClientsThisWeek: number;
  revenueThisWeek: number | null;
  revenueThisMonth: number | null;
}
```

**File:** `index.ts` (barrel export)

**Test cases:**
- No tests (type definitions only)

---

### F2 — Dashboard API service and store

Create the API service and NgRx SignalStore for the dashboard domain.

**File:** `libs/chairly/src/lib/dashboard/data-access/dashboard-api.service.ts`

```typescript
@Injectable({ providedIn: 'root' })
export class DashboardApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  getDashboard(): Observable<DashboardResponse> {
    return this.http.get<DashboardResponse>(`${this.baseUrl}/dashboard`);
  }
}
```

**File:** `libs/chairly/src/lib/dashboard/data-access/dashboard.store.ts`

Note: `DashboardStore` must NOT use `{ providedIn: 'root' }` — it is scoped to the route via `providers` in `dashboard.routes.ts` (see F5).

Store state:
```typescript
export interface DashboardState {
  dashboard: DashboardResponse | null;
  loading: boolean;
  error: string | null;
}
```

Store methods:
- `loadDashboard()`: calls `DashboardApiService.getDashboard()`, patches state with result. Sets `loading` to `true` before call and `false` after.

Computed signals:
- `isLoaded`: `dashboard() !== null`

**File:** `libs/chairly/src/lib/dashboard/data-access/index.ts` (barrel export)

**Test cases:**
- Vitest: `DashboardApiService` calls correct URL
- Vitest: `DashboardStore.loadDashboard()` sets loading state and patches dashboard

---

### F3 — Dashboard page component

Create the smart container component for the dashboard page.

**Folder:** `libs/chairly/src/lib/dashboard/feature/dashboard-page/`

**Files:**
- `dashboard-page.component.ts`
- `dashboard-page.component.html`

**Component:** `DashboardPageComponent` (smart/container)
- Selector: `chairly-dashboard-page`
- Standalone, OnPush
- Injects `DashboardStore` and `AuthStore` (from `@org/shared-lib`)
- `AuthStore.isOwner` and `AuthStore.isManager` are **computed signals** (defined via `withComputed` in `auth.store.ts`). Call them as `authStore.isOwner()` and `authStore.isManager()` in templates. Note: `isManager()` returns `true` for both `"manager"` and `"owner"` roles.
- On init: calls `store.loadDashboard()`
- Template delegates to presentational components (F4)

**Template structure (all copy in Dutch):**

```html
<div class="p-6 space-y-6">
  <!-- Page header -->
  <h1 class="text-2xl font-bold text-gray-900 dark:text-white">Dashboard</h1>

  <!-- Loading state -->
  @if (store.loading()) {
    <p class="text-gray-500 dark:text-gray-400">Laden...</p>
  }

  @if (store.dashboard(); as dashboard) {
    <!-- Stat cards row -->
    <chairly-dashboard-stats
      [todaysBookingsCount]="dashboard.todaysBookingsCount"
      [newClientsThisWeek]="dashboard.newClientsThisWeek"
      [revenueThisWeek]="dashboard.revenueThisWeek"
      [revenueThisMonth]="dashboard.revenueThisMonth"
      [isOwner]="authStore.isOwner()"
      [isManager]="authStore.isManager()" />

    <!-- Two-column layout for booking lists -->
    <div class="grid grid-cols-1 lg:grid-cols-2 gap-6">
      <chairly-dashboard-booking-list
        [title]="'Boekingen vandaag'"
        [bookings]="dashboard.todaysBookings"
        [emptyMessage]="'Geen boekingen vandaag'" />

      <chairly-dashboard-booking-list
        [title]="'Aankomende boekingen'"
        [bookings]="dashboard.upcomingBookings"
        [emptyMessage]="'Geen aankomende boekingen'" />
    </div>
  }

  <!-- Error state -->
  @if (store.error(); as error) {
    <p class="text-red-600 dark:text-red-400">{{ error }}</p>
  }
</div>
```

**Test cases:**
- Vitest: component creates and calls `loadDashboard()` on init
- Vitest: shows loading indicator when `loading` is true
- Vitest: renders stat cards and booking lists when data is loaded

---

### F4 — Dashboard presentational components

Create two presentational (dumb) UI components.

#### `chairly-dashboard-stats`

**Folder:** `libs/chairly/src/lib/dashboard/ui/dashboard-stats/`

**Files:**
- `dashboard-stats.component.ts`
- `dashboard-stats.component.html`

**Inputs (signal-based):**
- `todaysBookingsCount = input.required<number>()`
- `newClientsThisWeek = input.required<number>()`
- `revenueThisWeek = input.required<number | null>()`
- `revenueThisMonth = input.required<number | null>()`
- `isOwner = input.required<boolean>()`
- `isManager = input.required<boolean>()`

**Template (Dutch):**
- Grid of stat cards (responsive: 1 col on mobile, 2 on sm, 4 on lg)
- Each card: white background with dark mode variant, rounded, shadow, padding
- Card 1: "Boekingen vandaag" — shows `todaysBookingsCount` — visible to all roles
- Card 2: "Nieuwe klanten" — shows `newClientsThisWeek` — visible only if `isOwner() || isManager()` (use explicit OR; do not rely on `isManager` implicitly including Owner for readability)
- Card 3: "Omzet deze week" — shows `revenueThisWeek` formatted as currency (EUR) — visible only if `isOwner()`
- Card 4: "Omzet deze maand" — shows `revenueThisMonth` formatted as currency (EUR) — visible only if `isOwner()`
- Use `CurrencyPipe` with currency `'EUR'` for revenue formatting (the registered `LOCALE_ID` in `app.config.ts` handles locale formatting — do not pass `'nl-NL'` as a pipe argument)
- Dark mode: cards use `bg-white dark:bg-slate-800`, text uses `text-gray-900 dark:text-white` for values and `text-gray-700 dark:text-gray-300` for labels

**Test cases:**
- Vitest: renders all 4 cards for Owner
- Vitest: hides revenue cards for Manager
- Vitest: hides revenue and new clients cards for Staff Member
- Vitest: formats revenue as EUR currency

#### `chairly-dashboard-booking-list`

**Folder:** `libs/chairly/src/lib/dashboard/ui/dashboard-booking-list/`

**Files:**
- `dashboard-booking-list.component.ts`
- `dashboard-booking-list.component.html`

**Inputs (signal-based):**
- `title = input.required<string>()`
- `bookings = input.required<DashboardBooking[]>()`
- `emptyMessage = input.required<string>()`

**Template (Dutch):**
- Card container with title header
- If bookings empty: show `emptyMessage` in muted text
- If bookings present: list of booking rows, each showing:
  - Time: `startTime` formatted as `HH:mm` (use `DatePipe` with `'HH:mm'`)
  - Client name: `clientName`
  - Staff member name: `staffMemberName`
  - Services: `serviceNames` displayed using a local `JoinPipe` (see below) -- e.g. `{{ booking.serviceNames | join:', ' }}`. Do NOT use `serviceNames.join(", ")` in the template (no function calls in templates).
  - Status badge: replicate the `@switch (booking.status)` pattern from `bookings/ui/booking-table.component.html` with per-status colored `<span>` badges. The `BookingStatusPipe` lives in the `bookings` domain and cannot be imported (Sheriff boundary rules), so create a local `DashboardBookingStatusPipe` in `libs/chairly/src/lib/dashboard/pipes/dashboard-booking-status.pipe.ts` with the same `STATUS_LABELS` map and `bookingStatus` pipe name replaced by `dashboardBookingStatus`. Also create `libs/chairly/src/lib/dashboard/pipes/join.pipe.ts` for the `JoinPipe` (`name: 'join'`, transforms `string[]` to `string` with a configurable separator, default `', '`). Create barrel export at `libs/chairly/src/lib/dashboard/pipes/index.ts`.
- Dark mode: card `bg-white dark:bg-slate-800`, text colors with appropriate `dark:` variants
- Status translation for display (must match existing `BookingStatusPipe` labels): Scheduled -> "Gepland", Confirmed -> "Bevestigd", InProgress -> "Bezig", Completed -> "Voltooid", Cancelled -> "Geannuleerd", NoShow -> "Niet-verschenen"

**Test cases:**
- Vitest: renders title and booking list
- Vitest: shows empty message when bookings array is empty
- Vitest: displays time, client name, staff name, services for each booking

---

### F5 — Dashboard routing and app integration

Register the dashboard routes and update the app's default redirect.

**File:** `libs/chairly/src/lib/dashboard/dashboard.routes.ts`

```typescript
export const dashboardRoutes: Route[] = [
  {
    path: '',
    component: DashboardPageComponent,
    providers: [DashboardStore],
  },
];
```

Note: `DashboardApiService` is `providedIn: 'root'` and must NOT be listed in route providers (would create a duplicate instance).

**File:** `libs/chairly/src/index.ts`

Add export: `export { dashboardRoutes } from './lib/dashboard/dashboard.routes';`

**File:** `apps/chairly/src/app/app.routes.ts`

Changes:
1. Change the default redirect from `'diensten'` to `'dashboard'`
2. Add the dashboard route as a child of the shell:
```typescript
{
  path: 'dashboard',
  loadChildren: () => import('@org/chairly-lib').then((m) => m.dashboardRoutes),
},
```

**Barrel exports to create:**
- `libs/chairly/src/lib/dashboard/feature/index.ts` — exports `DashboardPageComponent`
- `libs/chairly/src/lib/dashboard/ui/index.ts` — exports both presentational components
- `libs/chairly/src/lib/dashboard/models/index.ts` — exports model interfaces

**Sheriff config:**
- Verify that the `dashboard` domain folder is picked up by the existing Sheriff glob patterns. If Sheriff uses explicit domain listing, add `dashboard` to the config.

**Test cases:**
- Vitest: route config loads `DashboardPageComponent`
- E2E (Playwright): navigating to `/` redirects to `/dashboard`
- E2E (Playwright): dashboard page loads and shows "Dashboard" heading
- E2E (Playwright): stat cards are visible based on role (Owner sees all)

---

### F6 — Playwright e2e tests for dashboard

Create end-to-end tests for the dashboard page.

**File:** `apps/chairly-e2e/src/dashboard.spec.ts`

**Scenarios:**
1. **Page load:** Navigate to `/dashboard`, verify heading "Dashboard" is visible
2. **Redirect:** Navigate to `/`, verify redirect to `/dashboard`
3. **Stat cards visible:** Verify "Boekingen vandaag" card is displayed
4. **Today's bookings section:** Verify "Boekingen vandaag" section heading is visible
5. **Upcoming bookings section:** Verify "Aankomende boekingen" section heading is visible
6. **Empty states:** When no bookings exist, verify empty messages are shown ("Geen boekingen vandaag", "Geen aankomende boekingen")

Follow existing Playwright test patterns in `apps/chairly-e2e/src/`.

**Test cases:**
- All scenarios listed above

## Acceptance Criteria

- [ ] `GET /api/dashboard` returns aggregated dashboard data for the current tenant
- [ ] Staff Member role sees only their own bookings in today's and upcoming lists
- [ ] Owner role sees revenue fields (week and month); other roles receive `null`
- [ ] Manager and Owner roles see new clients count; Staff Member receives `0`
- [ ] Upcoming bookings returns max 5, excludes cancelled/no-show/completed
- [ ] Revenue is calculated from paid, non-voided invoices only
- [ ] Frontend dashboard page is the new default route (redirects from `/`)
- [ ] All UI copy is in Dutch
- [ ] Dark mode styling works correctly on all dashboard components
- [ ] All backend quality checks pass (`dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`)
- [ ] All frontend quality checks pass (`lint`, `format:check`, `test`, `build`)
- [ ] Playwright e2e tests pass

## Out of Scope

- Real-time updates / WebSocket push for dashboard data
- Date range picker for custom dashboard periods
- Charts or graphs for revenue trends
- Clickable bookings that navigate to booking detail
- Dashboard widget configuration / customization
- Caching of dashboard queries
- Pagination on today's bookings list
- Export / print functionality
