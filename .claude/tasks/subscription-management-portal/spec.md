# Subscription Management Portal

## Overview

An internal platform administration portal for the Chairly SaaS owner to manage all subscriptions across all tenants. This is a brand-new Angular application (`chairly-admin`) within the existing Nx monorepo, backed by new admin API endpoints in the existing `Chairly.Api` project. The portal is protected by a dedicated Keycloak realm (`chairly-admin`) that is completely separate from tenant realms. The MVP scope covers subscription listing with search/filter, subscription detail view, and subscription lifecycle actions (provision, cancel, update plan/billing cycle).

## Domain Context

- Bounded context: **Admin** (new -- platform-level, not tenant-scoped)
- Key entities involved: **Subscription** (existing, in `WebsiteDbContext`)
- Ubiquitous language:
  - **Platform Admin** -- the Chairly SaaS owner who manages all tenant subscriptions
  - **Subscription** -- a tenant's contract to use the Chairly platform (existing entity from Onboarding context)
  - **Provision** -- the act of activating a subscription by setting `ProvisionedAtUtc`
  - **Cancel** -- the act of deactivating a subscription by setting `CancelledAtUtc` with a reason

### Subscription Status (derived from timestamps, per ADR-009)

| Status | Condition |
|---|---|
| Pending | `CreatedAtUtc` set, `ProvisionedAtUtc` null, `CancelledAtUtc` null, `TrialEndsAtUtc` null |
| Trial | `TrialEndsAtUtc` is not null, `ProvisionedAtUtc` null, `CancelledAtUtc` null |
| Provisioned | `ProvisionedAtUtc` set, `CancelledAtUtc` null |
| Cancelled | `CancelledAtUtc` set |

Note: A trial is also pending (not yet provisioned). In the admin list, both "Pending" and "Trial" are shown distinctly. The `IsTrial` derived property on the entity helps distinguish them.

---

## Infrastructure Tasks

### I1 — Keycloak admin realm dev seeder

Extend the existing `KeycloakDevSeeder` to also create a `chairly-admin` realm for the admin portal. This realm is separate from the tenant realm and has its own client and user.

**What to add to `KeycloakDevSeeder.cs`:**

After the existing tenant realm seeding (Step 1-4), add a new section that:

1. Creates a new realm `chairly-admin` with:
   - `displayName`: "Chairly Admin"
   - `loginTheme`: "chairly" (reuse existing theme)
   - `enabled`: true
   - A public client `chairly-admin-portal` (same pattern as `chairly-frontend`):
     - `publicClient`: true
     - `standardFlowEnabled`: true
     - `directAccessGrantsEnabled`: false
     - `redirectUris`: `["*"]`
     - `webOrigins`: `["*"]`
   - A realm role: `platform_admin`

2. Creates a default admin user:
   - Email: `admin@chairly.local`
   - Password: `ChairlyAdmin123!`
   - First name: `Platform`
   - Last name: `Admin`
   - Assign `platform_admin` realm role

**Configuration additions to `AppHost/Program.cs`:**

Add environment variables for the admin realm:
- `Keycloak__AdminPortalRealm`: `"chairly-admin"`
- `Keycloak__AdminPortalClientId`: `"chairly-admin-portal"`

**Configuration additions to `appsettings.json` / `appsettings.Development.json`:**

Add a new section:
```json
{
  "Keycloak": {
    "AdminPortalRealm": "chairly-admin",
    "AdminPortalClientId": "chairly-admin-portal"
  }
}
```

**Update JWT validation in `Program.cs`:**

The existing JWT Bearer auth validates that the issuer starts with `keycloakUrl + "/realms/"`. This already allows tokens from any realm on the same Keycloak instance, so the admin realm tokens will pass issuer validation automatically.

Add a new authorization policy:
```csharp
options.AddPolicy("RequirePlatformAdmin", p => p.RequireRole("platform_admin"));
```

Update `KeycloakRoleClaimTransformer` (or create a new one) to also recognize the `platform_admin` role from the `realm_access` claim. Currently, the `TenantContextMiddleware` extracts roles from `_knownRoles = ["owner", "manager", "staff_member"]`. The admin endpoints must NOT go through tenant resolution middleware (they are not tenant-scoped). This is handled by the endpoint routing -- admin endpoints skip the tenant middleware requirement (see B1).

**Update `_knownRoles` in relevant places:**

The `KeycloakRoleClaimTransformer` should recognize `platform_admin` as a valid role. However, the `TenantContextMiddleware` should NOT add it to its known roles -- admin requests should not attempt tenant resolution at all.

**Tests:**
- Verify the `chairly-admin` realm is created on dev startup
- Verify the admin user can authenticate against the `chairly-admin` realm
- Verify the `platform_admin` role is assigned

---

### I2 — Admin portal Angular application scaffolding

Create the new `chairly-admin` Angular application and `admin` domain library in the Nx monorepo.

**New app: `apps/chairly-admin/`**

Follow the exact same pattern as `apps/chairly-website/` with these differences:
- App name: `chairly-admin`
- Prefix: `chairly-admin`
- Port: `4400` (main app is 4200, website is 4300)
- Proxy config: `proxy.conf.json` proxying `/api` to `http://localhost:5000` (same as main app)

**Files to create:**
```
apps/chairly-admin/
  project.json
  tsconfig.json
  tsconfig.app.json
  tsconfig.spec.json
  eslint.config.mjs
  proxy.conf.json
  public/
    favicon.ico           (copy from main app)
  src/
    index.html
    main.ts
    styles.scss
    tailwind.css
    test-setup.ts
    app/
      app.component.ts
      app.component.html
      app.config.ts
      app.routes.ts
```

**`project.json`** -- follow `chairly-website/project.json` pattern:
- `name`: `"chairly-admin"`
- `prefix`: `"chairly-admin"`
- `sourceRoot`: `"apps/chairly-admin/src"`
- Build target: `@angular/build:application`
- `outputPath`: `"dist/apps/chairly-admin"`
- `styles`: `["apps/chairly-admin/src/tailwind.css", "apps/chairly-admin/src/styles.scss"]`
- Serve port: `4400`
- Proxy config pointing to backend

**`tailwind.css`** -- must include:
```css
@import 'tailwindcss';
@custom-variant dark (&:where([data-theme=dark], [data-theme=dark] *));
@source "../../libs/admin/src/**/*.{html,ts}";
@source "../../libs/shared/src/**/*.{html,ts}";
```

**`app.config.ts`** -- Keycloak-authenticated (same pattern as main `chairly` app):
- Register Dutch locale
- Provide `LOCALE_ID: 'nl-NL'`, `DEFAULT_CURRENCY_CODE: 'EUR'`
- Provide `KeycloakService` with `onLoad: 'login-required'`
- Fetch config from `/api/config/admin` (new endpoint, see B1)
- Use `authInterceptor` from `@org/shared-lib`

**`app.routes.ts`:**
```typescript
export const appRoutes: Route[] = [
  { path: '', redirectTo: 'abonnementen', pathMatch: 'full' },
  {
    path: '',
    component: AdminShellComponent,
    canActivate: [authGuard],
    children: [
      {
        path: 'abonnementen',
        loadChildren: () => import('@org/admin-lib').then((m) => m.subscriptionsRoutes),
      },
    ],
  },
];
```

**New lib: `libs/admin/`**

Follow the exact same structure as `libs/website/`:
```
libs/admin/
  project.json
  tsconfig.json
  tsconfig.lib.json
  tsconfig.spec.json
  eslint.config.mjs
  vite.config.mts
  src/
    index.ts
    test-setup.ts
    lib/
      layout/
        admin-shell/            (cross-domain shell component, see F3)
      subscriptions/
        models/
        data-access/
        feature/
        ui/
        pipes/
        subscriptions.routes.ts
```

**`project.json`** for the lib:
- `name`: `"admin"`
- `projectType`: `"library"`
- `sourceRoot`: `"libs/admin/src"`

**Path alias in `tsconfig.base.json`:**
```json
"@org/admin-lib": ["libs/admin/src/index.ts"]
```

**Sheriff config (`sheriff.config.ts`)** -- add:
```typescript
// In tagging:
'libs/admin/src': ['admin-lib'],
'libs/admin/src/lib': {
  'layout': ['admin-layout'],
  'subscriptions/<layer>': ['domain:subscriptions-admin', 'layer:<layer>'],
},

// In depRules:
root: ['chairly-lib', 'website-lib', 'admin-lib', 'shared'],
'admin-lib': ['domain:subscriptions-admin', 'admin-layout', 'shared'],
'admin-layout': ['shared'],
'domain:subscriptions-admin': [sameTag, 'admin-layout', 'shared'],
```

Note: Use `domain:subscriptions-admin` (not `domain:subscriptions`) to avoid conflicts with any future tenant-facing subscriptions domain in the `chairly-lib`. The `admin-layout` tag allows both the `admin-lib` barrel (for app.routes.ts) and domain layers (for imports in feature components) to access the shell component.

**E2E app: `apps/chairly-admin-e2e/`**

Create a Playwright e2e project following the same pattern as `apps/chairly-website-e2e/`.

**Tests:**
- Verify the admin app builds successfully (`nx build chairly-admin`)
- Verify lint passes (`nx lint chairly-admin`)

---

## Backend Tasks

### B1 — Admin endpoints infrastructure and GetAdminConfig

Set up the admin API route group and a config endpoint for the admin portal frontend.

**New file: `Chairly.Api/Features/Admin/AdminEndpoints.cs`**

```csharp
internal static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/subscriptions")
            .RequireAuthorization("RequirePlatformAdmin");

        group.MapGetAdminSubscriptionsList();
        group.MapGetAdminSubscription();
        group.MapProvisionSubscription();
        group.MapCancelAdminSubscription();
        group.MapUpdateSubscriptionPlan();

        return app;
    }
}
```

Note: The `/api/admin/*` endpoints use `RequireAuthorization("RequirePlatformAdmin")` which requires the `platform_admin` role. These endpoints do NOT go through tenant context resolution -- the `TenantContextMiddleware` already skips unauthenticated requests and only attempts tenant resolution for authenticated users with tenant realm tokens. Admin realm tokens will not have tenant claims, so the middleware must be updated to gracefully skip tenant resolution when the user has the `platform_admin` role (instead of returning 401).

**Update `TenantContextMiddleware`:**

Add a check at the top of `InvokeAsync`: if the user is authenticated and has the `platform_admin` role, skip tenant context population entirely and call `next(httpContext)`:
```csharp
if (httpContext.User.IsInRole("platform_admin"))
{
    await next(httpContext).ConfigureAwait(false);
    return;
}
```

This must come after the `IsAuthenticated` check and before the `TryPopulateTenantContext` call.

**New file: `Chairly.Api/Features/Config/GetAdminConfigEndpoint.cs`**

A new config endpoint that returns Keycloak settings for the admin portal:

```csharp
internal static class GetAdminConfigEndpoint
{
    public static void MapGetAdminConfig(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/config/admin", (IConfiguration configuration) =>
        {
            var keycloakUrl = configuration["Keycloak:Url"] ?? string.Empty;
            var realm = configuration["Keycloak:AdminPortalRealm"] ?? "chairly-admin";
            var clientId = configuration["Keycloak:AdminPortalClientId"] ?? "chairly-admin-portal";

            return Results.Ok(new
            {
                keycloakUrl,
                keycloakRealm = realm,
                keycloakClientId = clientId,
            });
        })
        .AllowAnonymous();
    }
}
```

**Update `Chairly.Api/Features/Config/` (wherever config endpoints are registered):**
- Add `app.MapGetAdminConfig();`

**Update `Program.cs`:**
- Add `using Chairly.Api.Features.Admin;`
- Add `app.MapAdminEndpoints();` after the other endpoint registrations

**Tests:**
- `GET /api/config/admin` returns 200 with keycloakUrl, keycloakRealm, keycloakClientId
- Admin endpoints return 401 for unauthenticated requests
- Admin endpoints return 403 for authenticated users without `platform_admin` role
- TenantContextMiddleware does not reject admin realm tokens

---

### B2 — GetAdminSubscriptionsList query, handler, and endpoint

List all subscriptions with search, filter, and pagination for the admin portal.

**Slice:** `Chairly.Api/Features/Admin/GetAdminSubscriptionsList/`

**Query -- `GetAdminSubscriptionsListQuery.cs`:**

Uses Data Annotations for input validation on `Page` and `PageSize`. The `ValidationBehavior` pipeline rejects invalid values with a 422 response automatically.

```csharp
internal sealed record GetAdminSubscriptionsListQuery(
    string? Search,
    string? Status,                                   // "pending", "trial", "provisioned", "cancelled"
    string? Plan,                                     // "starter", "team", "salon"
    [property: Range(1, int.MaxValue)] int Page,
    [property: Range(1, 100)] int PageSize) : IRequest<AdminSubscriptionsListResponse>;
```

Defaults (applied in the endpoint): `Page = 1`, `PageSize = 25`.

**Handler -- `GetAdminSubscriptionsListHandler.cs`:**
1. Inject `WebsiteDbContext` (NOT `ChairlyDbContext` -- subscriptions are in the website DB)
2. Build queryable on `Subscriptions` -- no tenant filter (admin sees all)
3. Apply filters:
   - `Search`: case-insensitive contains on `SalonName`, `Email`, `OwnerFirstName`, `OwnerLastName`
   - `Status` filter:
     - `"pending"`: `TrialEndsAtUtc == null && ProvisionedAtUtc == null && CancelledAtUtc == null`
     - `"trial"`: `TrialEndsAtUtc != null && ProvisionedAtUtc == null && CancelledAtUtc == null`
     - `"provisioned"`: `ProvisionedAtUtc != null && CancelledAtUtc == null`
     - `"cancelled"`: `CancelledAtUtc != null`
   - `Plan` filter: parse to `SubscriptionPlan` enum, filter on `Plan`
4. Order by `CreatedAtUtc` descending (newest first)
5. Apply pagination: `Skip((Page - 1) * PageSize).Take(PageSize)`
6. Return total count and items

**Response DTO -- `Chairly.Api/Features/Admin/AdminSubscriptionsListResponse.cs`:**
```csharp
internal sealed record AdminSubscriptionsListResponse(
    IReadOnlyList<AdminSubscriptionListItem> Items,
    int TotalCount,
    int Page,
    int PageSize);

internal sealed record AdminSubscriptionListItem(
    Guid Id,
    string SalonName,
    string OwnerName,          // "{FirstName} {LastName}"
    string Email,
    string Plan,               // lowercase slug
    string? BillingCycle,
    bool IsTrial,
    string Status,             // "pending", "trial", "provisioned", "cancelled"
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ProvisionedAtUtc,
    DateTimeOffset? CancelledAtUtc);
```

Status derivation logic in the mapper:
```csharp
static string DeriveStatus(Subscription s) =>
    s.CancelledAtUtc is not null ? "cancelled" :
    s.ProvisionedAtUtc is not null ? "provisioned" :
    s.TrialEndsAtUtc is not null ? "trial" :
    "pending";
```

**Endpoint -- `GetAdminSubscriptionsListEndpoint.cs`:**
- `GET /` (relative to the `/api/admin/subscriptions` group)
- Query params: `search`, `status`, `plan`, `page` (default 1), `pageSize` (default 25)
- 200 OK with `AdminSubscriptionsListResponse`
- 422 Unprocessable Entity for invalid `page` or `pageSize` (via `ValidationBehavior`)

**Tests:**
- Returns all subscriptions when no filters
- Search by salon name returns matching results
- Search by email returns matching results
- Filter by status "trial" returns only trial subscriptions
- Filter by status "provisioned" returns only provisioned subscriptions
- Filter by plan "starter" returns only starter subscriptions
- Pagination works correctly (page 1 returns first N, page 2 returns next N)
- Total count is correct regardless of pagination
- Results are ordered by CreatedAtUtc descending
- Page = 0 returns 422
- Page = -1 returns 422
- PageSize = 0 returns 422
- PageSize = 101 returns 422

---

### B3 — GetAdminSubscription query, handler, and endpoint

Get a single subscription by ID with full detail.

**Slice:** `Chairly.Api/Features/Admin/GetAdminSubscription/`

**Query -- `GetAdminSubscriptionQuery.cs`:**
```csharp
internal sealed record GetAdminSubscriptionQuery(Guid Id) : IRequest<OneOf<AdminSubscriptionDetailResponse, NotFound>>;
```

**Handler -- `GetAdminSubscriptionHandler.cs`:**
1. Inject `WebsiteDbContext`
2. Query `Subscriptions` by `Id` (no tenant filter)
3. Return `NotFound` if not found
4. Return full detail response

**Response DTO -- `Chairly.Api/Features/Admin/AdminSubscriptionDetailResponse.cs`:**
```csharp
internal sealed record AdminSubscriptionDetailResponse(
    Guid Id,
    string SalonName,
    string OwnerFirstName,
    string OwnerLastName,
    string Email,
    string? PhoneNumber,
    string Plan,                   // lowercase slug
    string? BillingCycle,
    bool IsTrial,
    string Status,                 // derived
    DateTimeOffset? TrialEndsAtUtc,
    DateTimeOffset CreatedAtUtc,
    Guid? CreatedBy,
    DateTimeOffset? ProvisionedAtUtc,
    Guid? ProvisionedBy,
    DateTimeOffset? CancelledAtUtc,
    Guid? CancelledBy,
    string? CancellationReason);
```

**Endpoint -- `GetAdminSubscriptionEndpoint.cs`:**
- `GET /{id:guid}` (relative to the `/api/admin/subscriptions` group)
- 200 OK with `AdminSubscriptionDetailResponse`
- 404 Not Found if subscription does not exist

**Tests:**
- Returns 200 with full subscription detail for existing subscription
- Returns 404 for non-existent subscription ID
- All fields are correctly mapped including derived Status and IsTrial

---

### B4 — ProvisionSubscription command, handler, and endpoint

Mark a subscription as provisioned (activated).

**Slice:** `Chairly.Api/Features/Admin/ProvisionSubscription/`

**Command -- `ProvisionSubscriptionCommand.cs`:**

The `Id` is bound from the `{id:guid}` route constraint, so no `[Required]` attribute is needed -- the route constraint guarantees a valid Guid.

```csharp
internal sealed class ProvisionSubscriptionCommand : IRequest<OneOf<AdminSubscriptionDetailResponse, NotFound, ValidationFailed>>
{
    public Guid Id { get; set; }
}
```

**Handler -- `ProvisionSubscriptionHandler.cs`:**
1. Inject `WebsiteDbContext` and `IHttpContextAccessor` (to get admin user ID from claims)
2. Find subscription by `Id`
3. Return `NotFound` if not found
4. Validate:
   - `ProvisionedAtUtc` must be null (not already provisioned). Return `ValidationFailed` with message "Abonnement is al geactiveerd." if already provisioned.
   - `CancelledAtUtc` must be null (not cancelled). Return `ValidationFailed` with message "Geannuleerd abonnement kan niet worden geactiveerd." if cancelled.
5. Set `ProvisionedAtUtc = DateTimeOffset.UtcNow`
6. Set `ProvisionedBy` to the admin user's ID (from `sub` claim)
7. Save changes
8. Return `AdminSubscriptionDetailResponse`

**Endpoint -- `ProvisionSubscriptionEndpoint.cs`:**
- `POST /{id:guid}/provision` (relative to the `/api/admin/subscriptions` group)
- 200 OK with `AdminSubscriptionDetailResponse`
- 404 Not Found
- 422 Unprocessable Entity for validation failures

**Tests:**
- Provisioning a pending subscription sets ProvisionedAtUtc and ProvisionedBy
- Provisioning an already-provisioned subscription returns 422
- Provisioning a cancelled subscription returns 422
- Provisioning a non-existent subscription returns 404
- ProvisionedBy matches the authenticated admin user ID

---

### B5 — CancelSubscription command, handler, and endpoint

Cancel a subscription with a mandatory reason.

**Slice:** `Chairly.Api/Features/Admin/CancelSubscription/`

**Command -- `CancelSubscriptionCommand.cs`:**

The `Id` is bound from the `{id:guid}` route constraint, so no `[Required]` attribute is needed on it. `CancellationReason` comes from the request body and requires validation annotations.

```csharp
internal sealed class CancelSubscriptionCommand : IRequest<OneOf<AdminSubscriptionDetailResponse, NotFound, ValidationFailed>>
{
    public Guid Id { get; set; }

    [Required, MaxLength(1000)]
    public string CancellationReason { get; set; } = string.Empty;
}
```

**Handler -- `CancelSubscriptionHandler.cs`:**
1. Find subscription by `Id`
2. Return `NotFound` if not found
3. Validate:
   - `CancelledAtUtc` must be null (not already cancelled). Return `ValidationFailed` with message "Abonnement is al geannuleerd." if already cancelled.
4. Set `CancelledAtUtc = DateTimeOffset.UtcNow`
5. Set `CancelledBy` to admin user's ID
6. Set `CancellationReason`
7. Save changes
8. Return `AdminSubscriptionDetailResponse`

**Endpoint -- `CancelSubscriptionEndpoint.cs`:**
- `POST /{id:guid}/cancel` (relative to the `/api/admin/subscriptions` group)
- Request body: `{ "cancellationReason": "..." }`
- 200 OK with `AdminSubscriptionDetailResponse`
- 404 Not Found
- 422 Unprocessable Entity for validation failures

**Tests:**
- Cancelling a pending subscription sets CancelledAtUtc, CancelledBy, CancellationReason
- Cancelling a provisioned subscription sets CancelledAtUtc, CancelledBy, CancellationReason
- Cancelling an already-cancelled subscription returns 422
- Cancelling a non-existent subscription returns 404
- CancellationReason is required (empty string returns 422)

---

### B6 — UpdateSubscriptionPlan command, handler, and endpoint

Update the plan and/or billing cycle of an existing subscription.

**Slice:** `Chairly.Api/Features/Admin/UpdateSubscriptionPlan/`

**Command -- `UpdateSubscriptionPlanCommand.cs`:**

The `Id` is bound from the `{id:guid}` route constraint. `Plan` and `BillingCycle` come from the request body.

```csharp
internal sealed class UpdateSubscriptionPlanCommand : IRequest<OneOf<AdminSubscriptionDetailResponse, NotFound, ValidationFailed>>
{
    public Guid Id { get; set; }

    [Required]
    public string Plan { get; set; } = string.Empty;          // "starter", "team", "salon"

    public string? BillingCycle { get; set; }                  // "Monthly", "Annual", or null
}
```

**Handler -- `UpdateSubscriptionPlanHandler.cs`:**
1. Find subscription by `Id`
2. Return `NotFound` if not found
3. Validate:
   - `CancelledAtUtc` must be null (cannot update cancelled subscription). Return `ValidationFailed` with message "Geannuleerd abonnement kan niet worden bijgewerkt."
   - `Plan` must parse to a valid `SubscriptionPlan` enum (case-insensitive). Return `ValidationFailed` if invalid.
   - `BillingCycle`, if not null, must parse to a valid `BillingCycle` enum (case-insensitive). Return `ValidationFailed` if invalid.
   - If subscription is a trial (`TrialEndsAtUtc != null`) and `BillingCycle` is provided: this converts the trial to a paid subscription. Set `TrialEndsAtUtc = null` and apply the new plan and billing cycle.
   - If subscription is paid (`TrialEndsAtUtc == null`) and `BillingCycle` is null: return `ValidationFailed` with message "Betaald abonnement vereist een factuurperiode."
4. Update `Plan` and `BillingCycle` on the entity
5. Save changes
6. Return `AdminSubscriptionDetailResponse`

**Endpoint -- `UpdateSubscriptionPlanEndpoint.cs`:**
- `PUT /{id:guid}/plan` (relative to the `/api/admin/subscriptions` group)
- Request body: `{ "plan": "team", "billingCycle": "Monthly" }`
- 200 OK with `AdminSubscriptionDetailResponse`
- 404 Not Found
- 422 Unprocessable Entity for validation failures

**Tests:**
- Updating plan from Starter to Team works
- Updating billing cycle from Monthly to Annual works
- Updating both plan and billing cycle in one request works
- Converting a trial to paid (providing billingCycle) clears TrialEndsAtUtc
- Updating a cancelled subscription returns 422
- Invalid plan string returns 422
- Invalid billingCycle string (e.g. "Weekly") returns 422
- Paid subscription with null billingCycle returns 422
- Non-existent subscription returns 404

---

## Frontend Tasks

### F1 — Admin domain library: subscription models and API service

Create the TypeScript models and API service for the admin subscription management.

**New file -- `libs/admin/src/lib/subscriptions/models/subscription.model.ts`:**
```typescript
export interface AdminSubscriptionListItem {
  id: string;
  salonName: string;
  ownerName: string;
  email: string;
  plan: string;
  billingCycle: string | null;
  isTrial: boolean;
  status: string;
  createdAtUtc: string;
  provisionedAtUtc: string | null;
  cancelledAtUtc: string | null;
}

export interface AdminSubscriptionsListResponse {
  items: AdminSubscriptionListItem[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface AdminSubscriptionDetail {
  id: string;
  salonName: string;
  ownerFirstName: string;
  ownerLastName: string;
  email: string;
  phoneNumber: string | null;
  plan: string;
  billingCycle: string | null;
  isTrial: boolean;
  status: string;
  trialEndsAtUtc: string | null;
  createdAtUtc: string;
  createdBy: string | null;
  provisionedAtUtc: string | null;
  provisionedBy: string | null;
  cancelledAtUtc: string | null;
  cancelledBy: string | null;
  cancellationReason: string | null;
}

export interface CancelSubscriptionPayload {
  cancellationReason: string;
}

export interface UpdateSubscriptionPlanPayload {
  plan: string;
  billingCycle: string | null;
}

export interface SubscriptionListFilters {
  search: string;
  status: string;
  plan: string;
  page: number;
  pageSize: number;
}
```

**New file -- `libs/admin/src/lib/subscriptions/models/index.ts`:**
Export all types from `subscription.model.ts`.

**New file -- `libs/admin/src/lib/subscriptions/data-access/admin-subscription-api.service.ts`:**
```typescript
@Injectable({ providedIn: 'root' })
export class AdminSubscriptionApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  getSubscriptions(filters: SubscriptionListFilters): Observable<AdminSubscriptionsListResponse> {
    const params = new HttpParams()
      .set('page', filters.page)
      .set('pageSize', filters.pageSize)
      .appendAll(filters.search ? { search: filters.search } : {})
      .appendAll(filters.status ? { status: filters.status } : {})
      .appendAll(filters.plan ? { plan: filters.plan } : {});
    return this.http.get<AdminSubscriptionsListResponse>(`${this.baseUrl}/admin/subscriptions`, { params });
  }

  getSubscription(id: string): Observable<AdminSubscriptionDetail> {
    return this.http.get<AdminSubscriptionDetail>(`${this.baseUrl}/admin/subscriptions/${id}`);
  }

  provisionSubscription(id: string): Observable<AdminSubscriptionDetail> {
    return this.http.post<AdminSubscriptionDetail>(`${this.baseUrl}/admin/subscriptions/${id}/provision`, {});
  }

  cancelSubscription(id: string, payload: CancelSubscriptionPayload): Observable<AdminSubscriptionDetail> {
    return this.http.post<AdminSubscriptionDetail>(`${this.baseUrl}/admin/subscriptions/${id}/cancel`, payload);
  }

  updateSubscriptionPlan(id: string, payload: UpdateSubscriptionPlanPayload): Observable<AdminSubscriptionDetail> {
    return this.http.put<AdminSubscriptionDetail>(`${this.baseUrl}/admin/subscriptions/${id}/plan`, payload);
  }
}
```

**New file -- `libs/admin/src/lib/subscriptions/data-access/admin-subscription.store.ts`:**

NgRx SignalStore for managing subscription list and detail state:
- State: `items`, `totalCount`, `page`, `pageSize`, `filters`, `isLoading`, `selectedSubscription`, `isDetailLoading`, `error`
- Methods:
  - `loadSubscriptions(filters)`: calls API, updates items/totalCount
  - `loadSubscription(id)`: calls API, sets selectedSubscription
  - `provisionSubscription(id)`: calls API, updates selectedSubscription and refreshes list
  - `cancelSubscription(id, payload)`: calls API, updates selectedSubscription and refreshes list
  - `updateSubscriptionPlan(id, payload)`: calls API, updates selectedSubscription and refreshes list

**New file -- `libs/admin/src/lib/subscriptions/data-access/index.ts`:**
Export the API service and store.

---

### F2 — Subscription status badge pipe

Create a pipe for displaying subscription status as a colored badge, following the same pattern as the invoice status badge pipe.

**New file -- `libs/admin/src/lib/subscriptions/pipes/subscription-status-badge.pipe.ts`:**

```typescript
@Pipe({ name: 'subscriptionStatusBadge', standalone: true })
export class SubscriptionStatusBadgePipe implements PipeTransform {
  transform(status: string): { label: string; cssClass: string } {
    switch (status) {
      case 'pending':
        return { label: 'In afwachting', cssClass: 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900 dark:text-yellow-200' };
      case 'trial':
        return { label: 'Proefperiode', cssClass: 'bg-blue-100 text-blue-800 dark:bg-blue-900 dark:text-blue-200' };
      case 'provisioned':
        return { label: 'Actief', cssClass: 'bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200' };
      case 'cancelled':
        return { label: 'Geannuleerd', cssClass: 'bg-red-100 text-red-800 dark:bg-red-900 dark:text-red-200' };
      default:
        return { label: status, cssClass: 'bg-gray-100 text-gray-800 dark:bg-gray-700 dark:text-gray-200' };
    }
  }
}
```

**New file -- `libs/admin/src/lib/subscriptions/pipes/index.ts`:**
Export the pipe.

---

### F3 — Admin shell component

Create the shell/layout component for the admin portal (sidebar navigation, header, content area).

**Location:** `libs/admin/src/lib/layout/admin-shell/` -- this is a cross-domain layout component that lives outside any specific domain folder. It is tagged as `admin-layout` in Sheriff (see I2) so that both the app routes and domain feature components can import it.

**New files:**
```
libs/admin/src/lib/layout/admin-shell/
  admin-shell.component.ts
  admin-shell.component.html
  admin-shell.component.scss
libs/admin/src/lib/layout/index.ts
```

**`libs/admin/src/lib/layout/index.ts`:** Export `AdminShellComponent`.

**`libs/admin/src/index.ts`:** Re-export from `./lib/layout` (so `app.routes.ts` can import `AdminShellComponent` from `@org/admin-lib`).

This is a simplified version of the main app's `ShellComponent`, tailored for the admin portal:
- Sidebar with navigation links (currently only "Abonnementen")
- Header bar with:
  - "Chairly Admin" branding/title
  - User name display (from AuthStore)
  - Dark mode toggle (reuse `ThemeService` from shared)
  - Logout button
- Content area with `<router-outlet />`
- Responsive: collapsible sidebar on mobile
- All text in Dutch

**Template structure:**
```html
<div class="flex h-screen bg-gray-50 dark:bg-slate-900">
  <!-- Sidebar -->
  <aside class="w-64 bg-white dark:bg-slate-800 border-r border-gray-200 dark:border-slate-700">
    <div class="p-4">
      <h1 class="text-xl font-bold text-gray-900 dark:text-white">Chairly Admin</h1>
    </div>
    <nav class="mt-4">
      <a routerLink="/abonnementen" routerLinkActive="..." class="...">
        Abonnementen
      </a>
    </nav>
  </aside>
  <!-- Main content -->
  <div class="flex-1 flex flex-col overflow-hidden">
    <header class="bg-white dark:bg-slate-800 border-b border-gray-200 dark:border-slate-700 px-6 py-4 flex items-center justify-between">
      <!-- Mobile menu toggle -->
      <!-- User info + theme toggle + logout -->
    </header>
    <main class="flex-1 overflow-y-auto p-6">
      <router-outlet />
    </main>
  </div>
</div>
```

---

### F4 — Subscription list page

Create the main subscription list page with search, filters, and pagination.

**New files:**
```
libs/admin/src/lib/subscriptions/feature/subscription-list-page/
  subscription-list-page.component.ts
  subscription-list-page.component.html
  subscription-list-page.component.scss
  subscription-list-page.component.spec.ts
```

**SubscriptionListPageComponent (smart, `feature/subscription-list-page/`):**
- `ChangeDetectionStrategy.OnPush`, standalone
- Injects `AdminSubscriptionStore`, `ActivatedRoute`, `Router`, `DestroyRef`
- On init: loads subscriptions with default filters
- Template structure:
  - Page header: "Abonnementen" with total count badge
  - Filter bar:
    - Search input (text): placeholder "Zoeken op salonnaam, e-mail, naam..."
    - Status dropdown: "Alle statussen", "In afwachting", "Proefperiode", "Actief", "Geannuleerd"
    - Plan dropdown: "Alle plannen", "Starter", "Team", "Salon"
  - Subscription table:
    - Columns: Salonnaam, Eigenaar, E-mail, Plan, Status, Aangemaakt, Acties
    - Status column uses `subscriptionStatusBadge` pipe
    - Plan column shows plan name capitalized
    - Aangemaakt column shows formatted date
    - Acties column: "Bekijken" link to detail page
  - Pagination controls at the bottom:
    - "Vorige" / "Volgende" buttons
    - Page indicator: "Pagina X van Y"
    - Items per page selector: 10, 25, 50
  - Empty state: "Geen abonnementen gevonden." when no results
  - Loading state: loading indicator during API calls
- All filter changes trigger a new API call (debounce search input by 300ms)

**URL query param synchronization (managed in the smart component, not the store):**

1. **Read on init:** In `ngOnInit`, read current filter values from `ActivatedRoute.snapshot.queryParams`. Map query param keys (`search`, `status`, `plan`, `page`, `pageSize`) to the `SubscriptionListFilters` interface, using defaults for missing params (`search: ''`, `status: ''`, `plan: ''`, `page: 1`, `pageSize: 25`). Pass the resulting filters to `store.loadSubscriptions(filters)`.

2. **Write on change:** When any filter value changes (search input after 300ms debounce, dropdown change, pagination change), the component:
   - Calls `Router.navigate([], { queryParams, queryParamsHandling: 'merge', replaceUrl: true })` to update the URL without triggering navigation. Only include non-default values in `queryParams` (omit `search` if empty, omit `page` if 1, etc.) to keep the URL clean.
   - Calls `store.loadSubscriptions(filters)` with the new filter state.

3. **Search debounce:** Use a `Subject<string>` for the search input, piped through `debounceTime(300)` and `distinctUntilChanged()`, subscribed with `takeUntilDestroyed(destroyRef)`. When the debounced value emits, update URL and trigger API call as described above.

This approach keeps the store stateless regarding URL concerns -- the store only knows about loading data, the component owns the URL synchronization.

**Route:** In `subscriptions.routes.ts`:
```typescript
export const subscriptionsRoutes: Route[] = [
  { path: '', component: SubscriptionListPageComponent },
  { path: ':id', component: SubscriptionDetailPageComponent },
];
```

---

### F5 — Subscription detail page

Create the detail page showing full subscription info with action buttons.

**New files:**
```
libs/admin/src/lib/subscriptions/feature/subscription-detail-page/
  subscription-detail-page.component.ts
  subscription-detail-page.component.html
  subscription-detail-page.component.scss
  subscription-detail-page.component.spec.ts
```

**SubscriptionDetailPageComponent (smart, `feature/subscription-detail-page/`):**
- `ChangeDetectionStrategy.OnPush`, standalone
- Injects `AdminSubscriptionStore`, `ActivatedRoute`, `Router`, `DestroyRef`
- On init: reads `:id` from route params, loads subscription detail
- Template structure:
  - Back link: "Terug naar overzicht" linking to `/abonnementen`
  - Page header: salon name with status badge
  - Detail card with two columns:
    - Left column -- Abonnementgegevens:
      - Plan: plan name (e.g. "Starter", "Team", "Salon")
      - Factuurperiode: "Maandelijks" / "Jaarlijks" / "N.v.t." (for trials)
      - Proefperiode: trial end date or "Geen proefperiode"
      - Status: badge
    - Right column -- Contactgegevens:
      - Salonnaam
      - Eigenaar: full name
      - E-mailadres
      - Telefoonnummer (or "Niet opgegeven")
  - Timeline section -- Tijdlijn:
    - Show timestamp events in chronological order:
      - "Aangemaakt op {date}" (always present)
      - "Geactiveerd op {date} door {userId}" (if provisioned)
      - "Geannuleerd op {date} door {userId}" (if cancelled)
      - "Reden: {cancellationReason}" (if cancelled)
  - Action buttons section (conditionally shown based on status):
    - **Pending/Trial status**: Show "Activeren" button (primary) and "Annuleren" button (danger)
    - **Provisioned status**: Show "Plan wijzigen" button and "Annuleren" button (danger)
    - **Cancelled status**: No action buttons, show "Dit abonnement is geannuleerd." info text

**Action: Activeren (Provision)**
- Clicking "Activeren" opens the `ProvisionSubscriptionDialogComponent` (see F6)
- On confirm: calls `store.provisionSubscription(id)`, shows success feedback, refreshes detail

**Action: Annuleren (Cancel)**
- Clicking "Annuleren" opens the `CancelSubscriptionDialogComponent` (see F7)
- On confirm: calls `store.cancelSubscription(id, { cancellationReason })`, shows success feedback, refreshes detail

**Action: Plan wijzigen (Update Plan)**
- Clicking "Plan wijzigen" opens the `UpdatePlanDialogComponent` (see F8)
- On confirm: calls `store.updateSubscriptionPlan(id, { plan, billingCycle })`, shows success feedback, refreshes detail

---

### F6 — Provision subscription dialog component

Create a confirmation dialog component for the provision action.

**New files:**
```
libs/admin/src/lib/subscriptions/ui/provision-subscription-dialog/
  provision-subscription-dialog.component.ts
  provision-subscription-dialog.component.html
  provision-subscription-dialog.component.scss
  provision-subscription-dialog.component.spec.ts
```

**ProvisionSubscriptionDialogComponent (presentational, `ui/provision-subscription-dialog/`):**
- Uses native `<dialog>` with `showModal()` pattern per CLAUDE.md
- Inputs (signal-based):
  - `salonName = input.required<string>()` (displayed in the confirmation message)
  - `isSubmitting = input<boolean>(false)`
- Outputs (signal-based, using Angular `output()` factory function):
  - `confirm = output<void>()`
  - `cancel = output<void>()`
- Methods:
  - `open()`: calls `dialog.showModal()`, sets `document.body.style.overflow = 'hidden'`
  - `close()`: calls `dialog.close()`, sets `document.body.style.overflow = ''`
- Dialog text: "Weet u zeker dat u het abonnement voor {salonName} wilt activeren?"
- Confirm button: "Activeren" (primary styled)
- Cancel button: "Annuleren"
- All text in Dutch

---

### F7 — Cancel subscription dialog component

Create a reusable dialog component for the cancel action.

**New files:**
```
libs/admin/src/lib/subscriptions/ui/cancel-subscription-dialog/
  cancel-subscription-dialog.component.ts
  cancel-subscription-dialog.component.html
  cancel-subscription-dialog.component.scss
  cancel-subscription-dialog.component.spec.ts
```

**CancelSubscriptionDialogComponent (presentational, `ui/cancel-subscription-dialog/`):**
- Uses native `<dialog>` with `showModal()` pattern per CLAUDE.md
- Inputs (signal-based):
  - `isSubmitting = input<boolean>(false)`
- Outputs (signal-based, using Angular `output()` factory function):
  - `confirm = output<string>()` (emits the cancellation reason)
  - `cancel = output<void>()`
- Reactive form with a single `cancellationReason` FormControl (required, maxLength 1000)
- Methods:
  - `open()`: calls `dialog.showModal()`, sets `document.body.style.overflow = 'hidden'`
  - `close()`: calls `dialog.close()`, resets form, sets `document.body.style.overflow = ''`
- All text in Dutch

---

### F8 — Update plan dialog component

Create a reusable dialog component for the plan update action.

**New files:**
```
libs/admin/src/lib/subscriptions/ui/update-plan-dialog/
  update-plan-dialog.component.ts
  update-plan-dialog.component.html
  update-plan-dialog.component.scss
  update-plan-dialog.component.spec.ts
```

**UpdatePlanDialogComponent (presentational, `ui/update-plan-dialog/`):**
- Uses native `<dialog>` with `showModal()` pattern per CLAUDE.md
- Inputs (signal-based):
  - `currentPlan = input.required<string>()`
  - `currentBillingCycle = input.required<string | null>()`
  - `isSubmitting = input<boolean>(false)`
- Outputs (signal-based, using Angular `output()` factory function):
  - `confirm = output<UpdateSubscriptionPlanPayload>()`
  - `cancel = output<void>()`
- Reactive form:
  - `plan`: dropdown with options Starter / Team / Salon (values: "starter", "team", "salon")
  - `billingCycle`: dropdown with options Maandelijks / Jaarlijks (values: "Monthly", "Annual")
- When opened, preselects current values
- All text in Dutch:
  - Plan label: "Plan"
  - Billing cycle label: "Factuurperiode"
  - Dropdown options: "Starter", "Team", "Salon" / "Maandelijks", "Jaarlijks"

---

### F9 — E2E tests for admin portal

Write Playwright e2e tests for the admin portal.

**New files:**
```
apps/chairly-admin-e2e/src/
  subscription-list.spec.ts
  subscription-detail.spec.ts
```

**`subscription-list.spec.ts`:**
- Navigate to `/abonnementen`
- Verify page heading "Abonnementen" is visible
- Mock `GET /api/admin/subscriptions` to return test data
- Verify table displays subscription data
- Verify search input filters results (with debounce)
- Verify status dropdown filters results
- Verify plan dropdown filters results
- Verify pagination controls work
- Verify empty state message when no results
- Verify clicking "Bekijken" navigates to detail page
- Verify URL query params update when filters change

**`subscription-detail.spec.ts`:**
- Mock `GET /api/admin/subscriptions/{id}` to return test data
- Navigate to `/abonnementen/{id}`
- Verify detail information is displayed correctly
- Verify status badge shows correct status
- Verify timeline events are shown
- Test provision action:
  - Click "Activeren", verify confirmation dialog with salon name
  - Mock `POST /api/admin/subscriptions/{id}/provision`
  - Confirm, verify success feedback
- Test cancel action:
  - Click "Annuleren", verify dialog with textarea
  - Enter reason, confirm
  - Mock `POST /api/admin/subscriptions/{id}/cancel`
  - Verify success feedback
- Test update plan action:
  - Click "Plan wijzigen", verify dialog with dropdowns
  - Change selections, confirm
  - Mock `PUT /api/admin/subscriptions/{id}/plan`
  - Verify success feedback
- Verify "Terug naar overzicht" link works
- Verify cancelled subscription shows no action buttons

---

## Acceptance Criteria

- [ ] Keycloak `chairly-admin` realm is created by dev seeder with `platform_admin` role and admin user
- [ ] `GET /api/config/admin` returns admin portal Keycloak config (anonymous access)
- [ ] `GET /api/admin/subscriptions` lists all subscriptions with search, filter, and pagination (requires `platform_admin` role)
- [ ] `GET /api/admin/subscriptions` returns 422 for invalid page/pageSize values
- [ ] `GET /api/admin/subscriptions/{id}` returns full subscription detail (requires `platform_admin` role)
- [ ] `POST /api/admin/subscriptions/{id}/provision` provisions a subscription (requires `platform_admin` role)
- [ ] `POST /api/admin/subscriptions/{id}/cancel` cancels a subscription with reason (requires `platform_admin` role)
- [ ] `PUT /api/admin/subscriptions/{id}/plan` updates plan and billing cycle (requires `platform_admin` role)
- [ ] Admin endpoints return 401 for unauthenticated requests
- [ ] Admin endpoints return 403 for non-admin authenticated users
- [ ] TenantContextMiddleware gracefully skips tenant resolution for `platform_admin` role tokens
- [ ] `chairly-admin` Angular app builds and serves on port 4400
- [ ] Admin app authenticates against `chairly-admin` Keycloak realm
- [ ] Subscription list page displays with search, status filter, plan filter, and pagination
- [ ] Subscription list page reads filters from URL query params on load and writes changes back
- [ ] Subscription detail page displays full information with timeline
- [ ] Provision, cancel, and plan update actions work via dedicated dialog components
- [ ] Plan and billing cycle selection use dropdowns, not raw text inputs
- [ ] All user-facing text is in Dutch
- [ ] All frontend components use signal-based API (`input()`, `output()`)
- [ ] Dark mode works correctly in admin portal
- [ ] All backend quality checks pass (dotnet build, test, format)
- [ ] All frontend quality checks pass (lint, test, build)
- [ ] Playwright e2e tests pass for subscription list and detail pages

## Out of Scope

- Tenant monitoring and health dashboards (future feature)
- Tenant self-service subscription management via the Chairly website (future feature)
- Automated tenant provisioning based on subscription activation
- Trial expiry enforcement / automated reminders
- Stripe payment integration
- Usage-based billing or overage charges
- Custom/enterprise plan tier
- Email verification during sign-up
- Admin user management (adding more platform admins)
- Audit log for admin actions
- Subscription analytics / reporting dashboard
