# Authentication & Authorization

> **Status: Implemented** — Merged to main.

## Overview

This feature wires up end-to-end authentication and authorization for Chairly using Keycloak as
the identity provider (ADR-008). Each tenant gets its own Keycloak realm (realm-per-tenant,
ADR-008). The realm name equals the tenant's `TenantId` (Guid as string). The realm is configured
at deploy time via environment variables -- users never enter a tenant identifier manually.

The feature covers: Keycloak container in Aspire, dynamic multi-issuer JWT validation, tenant
context middleware (replacing the `TenantConstants.DefaultTenantId` placeholder), role-based
authorization policies applied to all existing endpoints, a Keycloak Admin API service,
a tenant provisioning endpoint (automation hook for future self-service onboarding),
staff-Keycloak user sync, a runtime config endpoint the Angular app calls at startup,
and the Angular keycloak-angular integration including Bearer interceptor and auth guards.

## Domain Context

- Bounded context: all (auth is cross-cutting)
- Key entities involved: `StaffMember` (gains `Email` + `KeycloakUserId` fields)
- Ubiquitous language: Owner, Manager, Staff Member (roles); Tenant (realm in Keycloak)

## Configuration

All Keycloak settings flow through `IConfiguration` under the `Keycloak` section:

| Key | Example | Description |
|---|---|---|
| `Keycloak:Url` | `http://localhost:8080` | Base URL of the Keycloak server |
| `Keycloak:Realm` | `00000000-0000-0000-0000-000000000001` | The tenant's realm name (= TenantId as string) |
| `Keycloak:ClientId` | `chairly-frontend` | OIDC client ID used by the Angular app |
| `Keycloak:AdminClientId` | `chairly-admin` | Service account client ID for Admin API calls |
| `Keycloak:AdminClientSecret` | `...` | Service account secret (mark as secret in AppHost) |

In local development the AppHost injects these automatically from the running Keycloak container.
In production they are set as deployment environment variables.

## Backend Tasks

### B1 — Keycloak container in AppHost

Add Keycloak to `Chairly.AppHost/Program.cs` as a Docker container resource.

**Container:**
```
image: quay.io/keycloak/keycloak  (use a pinned recent tag, e.g. 26.2)
command: start-dev
env: KEYCLOAK_ADMIN=admin, KEYCLOAK_ADMIN_PASSWORD=admin
port: 8080 (HTTP)
data volume: keycloak-data (persists realm config across restarts)
```

**AppHost wiring:**
- Add a `keycloakAdminPassword` parameter marked as secret
- Pass `Keycloak__Url`, `Keycloak__Realm`, `Keycloak__ClientId`,
  `Keycloak__AdminClientId`, `Keycloak__AdminClientSecret` as environment variables to the API
  project using `WithEnvironment()`
- `Keycloak__Realm` defaults to `00000000-0000-0000-0000-000000000001` for local development
  (the default tenant used by `TenantConstants.DefaultTenantId`)
- The API project should `.WaitFor(keycloak)` before starting
- Add a `DisplayText = "Keycloak Admin"` URL annotation pointing to `/admin` on the Keycloak
  resource so the Aspire dashboard shows a quick link

**Keycloak initial setup (documented, not automated in this spec):**
After first run, an operator must manually:
1. Create a realm named `00000000-0000-0000-0000-000000000001`
2. Create a public OIDC client named `chairly-frontend` with:
   - Standard flow enabled
   - Valid redirect URIs: `http://localhost:4200/*`
   - Web origins: `http://localhost:4200`
3. Create a service account client named `chairly-admin` with:
   - Client credentials grant enabled
   - Realm management roles: `manage-users`, `manage-realm`
4. Create realm roles: `owner`, `manager`, `staff_member`
5. Create a test user and assign role `owner`

Document these steps in a new file `docs/keycloak-setup.md`.

**Tests:** No unit tests needed for AppHost configuration. Verify manually that the container
starts and the Admin UI is reachable via the Aspire dashboard.

---

### B2 — StaffMember entity: add Email and KeycloakUserId

The `StaffMember` entity in `Chairly.Domain` needs two new fields required for Keycloak
user provisioning.

**Domain changes (`Chairly.Domain/Entities/StaffMember.cs`):**
```
Email: string (required, max 256)
KeycloakUserId: string? (nullable -- set after successful Keycloak provisioning)
```

**EF configuration (`Chairly.Infrastructure`):**
- `Email`: `HasMaxLength(256)`, `IsRequired()`, add unique index per tenant:
  `HasIndex(x => new { x.TenantId, x.Email }).IsUnique()`
- `KeycloakUserId`: `HasMaxLength(256)`, nullable

**Migration (idempotent -- follow CLAUDE.md migration rules):**
- `Email` column: use `DO $$ BEGIN IF NOT EXISTS ... THEN ALTER TABLE "StaffMembers" ADD COLUMN "Email" varchar(256) NOT NULL DEFAULT ''; END IF; END $$;`
  The default empty string handles existing rows in dev; it is NOT meaningful and only prevents
  a NOT NULL constraint failure during migration
- `KeycloakUserId` column: nullable, no default needed
- Unique index: `CREATE UNIQUE INDEX IF NOT EXISTS "IX_StaffMembers_TenantId_Email" ON "StaffMembers" ("TenantId", "Email")`

**Command update (`CreateStaffMemberCommand`):**
Add `Email` field: `[Required] [EmailAddress] [MaxLength(256)]`

**Response update (`StaffMemberResponse`):**
Add `Email: string` to the record/response.

**Handler updates:**
- `CreateStaffMemberHandler`: set `Email = command.Email` when constructing the entity
- `UpdateStaffMemberHandler` / `UpdateStaffMemberCommand`: add optional `Email` field for updates
- All handlers that return `StaffMemberResponse`: include `Email` in mapping

**Tests:**
- Unit test: `CreateStaffMemberHandler` sets `Email` correctly
- Unit test: mapping includes `Email` in `StaffMemberResponse`

---

### B3 — Dynamic JWT Bearer authentication + tenant context middleware

**NuGet packages to add to `Chairly.Api`:**
- `Microsoft.AspNetCore.Authentication.JwtBearer`
- `Microsoft.IdentityModel.Protocols.OpenIdConnect`

**`ITenantContext` interface (`Chairly.Api/Shared/Tenancy/ITenantContext.cs`):**
```csharp
public interface ITenantContext
{
    Guid TenantId { get; }
    Guid UserId { get; }
    string UserRole { get; }  // "owner" | "manager" | "staff_member"
}
```

**`TenantContext` implementation** (mutable, scoped):
```csharp
internal sealed class TenantContext : ITenantContext
{
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public string UserRole { get; set; } = string.Empty;
}
```

**`TenantContextMiddleware` (`Chairly.Api/Shared/Tenancy/TenantContextMiddleware.cs`):**
- Reads the JWT from `HttpContext.User` (populated by JWT Bearer middleware)
- Extracts the `iss` claim: format is `{KeycloakUrl}/realms/{realmName}`
- Parses `realmName` as a Guid -> sets `TenantContext.TenantId`
- Extracts `sub` claim -> sets `TenantContext.UserId`
- Extracts `realm_access.roles` from the JWT (Keycloak stores realm roles here as a JSON array
  inside a claim named `realm_access`) -> sets `TenantContext.UserRole`
  (first matching value from `["owner", "manager", "staff_member"]`)
- If any extraction fails (missing claim, invalid Guid), return `401 Unauthorized`
- Middleware runs AFTER `UseAuthentication()` and `UseAuthorization()`

**JWT Bearer configuration in `Program.cs`:**
```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var keycloakUrl = builder.Configuration["Keycloak:Url"]!;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidAudience = "account",  // Keycloak default audience
            // Validate that the issuer belongs to our Keycloak instance
            IssuerValidator = (issuer, _, _) =>
            {
                if (issuer.StartsWith(keycloakUrl + "/realms/", StringComparison.Ordinal))
                    return issuer;
                throw new SecurityTokenInvalidIssuerException("Untrusted issuer");
            },
            // Dynamically resolve signing keys per realm (JWKS fetched per issuer)
            IssuerSigningKeyResolver = (_, securityToken, kid, _) =>
            {
                var jwksUrl = securityToken.Issuer + "/protocol/openid-connect/certs";
                // Use a cached IConfigurationManager<OpenIdConnectConfiguration> per issuer
                // (see implementation note below)
            },
        };
    });
```

**Implementation note on key caching:**
Create a `KeycloakJwksCache` service (singleton) that maintains a
`ConcurrentDictionary<string, ConfigurationManager<OpenIdConnectConfiguration>>` keyed by realm
issuer URL. On `IssuerSigningKeyResolver`, retrieve or create a
`ConfigurationManager<OpenIdConnectConfiguration>` for the issuer, call
`GetConfigurationAsync()`, and return `configuration.SigningKeys`. This ensures JWKS are cached
per realm and refreshed automatically when keys rotate.

**`Program.cs` pipeline order:**
```
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<TenantContextMiddleware>();
// ... endpoint mapping
```

**DI registration:**
```csharp
builder.Services.AddScoped<TenantContext>();
builder.Services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());
builder.Services.AddSingleton<KeycloakJwksCache>();
```

**Replace `TenantConstants.DefaultTenantId`:**
All handlers currently using `TenantConstants.DefaultTenantId` must be updated to inject
`ITenantContext` and use `tenantContext.TenantId` instead. Same for `CreatedBy = Guid.Empty`
-- replace with `tenantContext.UserId`. Remove the `#pragma warning disable MA0026` suppressions.
`TenantConstants.cs` can be deleted once all usages are removed.

**Tests:**
- Unit test: `TenantContextMiddleware` correctly parses `TenantId` from issuer, `UserId` from sub,
  `UserRole` from realm_access.roles
- Unit test: middleware returns 401 for malformed issuer
- Unit test: middleware returns 401 for unknown issuer domain

---

### B4 — Authorization policies + secure all endpoints

**Policies in `Program.cs`:**
```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireOwner",   p => p.RequireRole("owner"));
    options.AddPolicy("RequireManager", p => p.RequireRole("owner", "manager"));
    options.AddPolicy("RequireStaff",   p => p.RequireRole("owner", "manager", "staff_member"));
});
```

Keycloak realm roles map to ASP.NET Core roles via a claim transformation. Add a
`KeycloakRoleClaimTransformer : IClaimsTransformation` that reads `realm_access` from the JWT and
adds each role as a `ClaimTypes.Role` claim. Register as:
`builder.Services.AddScoped<IClaimsTransformation, KeycloakRoleClaimTransformer>()`

**Endpoint authorization matrix:**

| Endpoint group | Policy |
|---|---|
| Bookings (all) | `RequireStaff` |
| Clients (all) | `RequireStaff` |
| Services + ServiceCategories (read) | `RequireStaff` |
| Services + ServiceCategories (write) | `RequireManager` |
| Staff (read) | `RequireStaff` |
| Staff (write: create/update/deactivate/reactivate) | `RequireManager` |
| Settings (read) | `RequireStaff` |
| Settings (write) | `RequireOwner` |
| Billing / Invoices (all) | `RequireManager` |
| Notifications (all) | `RequireStaff` |
| `GET /config` (B8) | no auth -- public endpoint |
| `POST /tenants` (B6) | no auth -- provisioning endpoint (secured by network/ops, not JWT) |

Apply via `.RequireAuthorization("PolicyName")` on each endpoint group in the `Map*Endpoints()`
extension methods.

**Tests:**
- Integration test: unauthenticated request to a protected endpoint returns 401
- Integration test: request with `owner` role token can access Owner-only endpoints
- Integration test: request with `staff_member` role token receives 403 on Manager-only endpoints

---

### B5 — Keycloak Admin API service

Create `Chairly.Infrastructure/Keycloak/IKeycloakAdminService.cs` and
`Chairly.Infrastructure/Keycloak/KeycloakAdminService.cs`.

**`IKeycloakAdminService` interface:**
```csharp
public interface IKeycloakAdminService
{
    Task<string> CreateRealmAsync(Guid tenantId, string adminEmail, CancellationToken ct = default);
    Task<string> CreateUserAsync(Guid tenantId, string email, string firstName, string lastName,
        string role, CancellationToken ct = default);  // returns Keycloak user ID
    Task UpdateUserAsync(Guid tenantId, string keycloakUserId, string email,
        string firstName, string lastName, CancellationToken ct = default);
    Task DisableUserAsync(Guid tenantId, string keycloakUserId, CancellationToken ct = default);
    Task EnableUserAsync(Guid tenantId, string keycloakUserId, CancellationToken ct = default);
    Task DeleteUserAsync(Guid tenantId, string keycloakUserId, CancellationToken ct = default);
    Task AssignRealmRoleAsync(Guid tenantId, string keycloakUserId, string roleName,
        CancellationToken ct = default);
}
```

**`KeycloakAdminService` implementation:**
- Inject `IHttpClientFactory` (named client `keycloak-admin`)
- Obtain a service account token via `POST {Keycloak:Url}/realms/master/protocol/openid-connect/token`
  with `client_credentials` grant using `Keycloak:AdminClientId` / `Keycloak:AdminClientSecret`
- Cache the token until it expires (store expiry time, refresh if within 30 s of expiry)
- All realm operations target `{Keycloak:Url}/admin/realms/{tenantId}`
- `CreateRealmAsync`: `POST /admin/realms` with a minimal realm representation:
  `{ "realm": "{tenantId}", "enabled": true, "clients": [...frontend + admin clients] }`
  The clients array should pre-configure `chairly-frontend` (public OIDC) and `chairly-admin`
  (service account) so the new realm is usable immediately
- `CreateUserAsync`: `POST /admin/realms/{tenantId}/users` -> `{ "username": email, "email": email,
  "firstName": firstName, "lastName": lastName, "enabled": true,
  "requiredActions": ["UPDATE_PASSWORD"] }`
  Returns the Keycloak user ID from the `Location` response header
- `AssignRealmRoleAsync`: get role representation first (`GET /admin/realms/{tenantId}/roles/{roleName}`),
  then `POST /admin/realms/{tenantId}/users/{userId}/role-mappings/realm`
- `UpdateUserAsync`: `PUT /admin/realms/{tenantId}/users/{keycloakUserId}`
- `DisableUserAsync` / `EnableUserAsync`: `PUT /admin/realms/{tenantId}/users/{keycloakUserId}`
  with `{ "enabled": false/true }`
- `DeleteUserAsync`: `DELETE /admin/realms/{tenantId}/users/{keycloakUserId}`

**Registration in `Program.cs`:**
```csharp
builder.Services.AddHttpClient("keycloak-admin");
builder.Services.AddSingleton<IKeycloakAdminService, KeycloakAdminService>();
```

**Tests:**
- Unit test with mocked `HttpMessageHandler`: `CreateUserAsync` sends correct request body and
  parses user ID from `Location` header
- Unit test: token caching -- second call within token lifetime does NOT re-fetch token
- Unit test: token refresh when near expiry

---

### B6 — Tenant provisioning endpoint

`POST /tenants` -- creates a Keycloak realm and an Owner user for a new tenant.
This endpoint is the automation hook for future self-service onboarding. It is NOT
JWT-protected (secured at the network/ops level in production; e.g., only callable from
an internal provisioning service).

**Command (`Chairly.Api/Features/Tenants/ProvisionTenant/ProvisionTenantCommand.cs`):**
```
TenantId: Guid (required)
OwnerEmail: string (required, email format, max 256)
OwnerFirstName: string (required, max 100)
OwnerLastName: string (required, max 100)
```

**Handler (`ProvisionTenantHandler`):**
1. Call `IKeycloakAdminService.CreateRealmAsync(command.TenantId, command.OwnerEmail)`
2. Call `IKeycloakAdminService.CreateUserAsync(...)` for the owner -> get `keycloakUserId`
3. Call `IKeycloakAdminService.AssignRealmRoleAsync(..., "owner")`
4. Return `201 Created` with body:
   ```json
   {
     "tenantId": "...",
     "ownerKeycloakUserId": "...",
     "loginUrl": "{Keycloak:Url}/realms/{tenantId}/account"
   }
   ```
5. On any Keycloak error: attempt cleanup (delete realm if it was created) and return
   `502 Bad Gateway` with a problem details body

**Endpoint:** `POST /tenants` -- no `.RequireAuthorization()`

**Validator:**
- `TenantId` must not be `Guid.Empty`
- `OwnerEmail` must be valid email format

**Tests:**
- Unit test: happy path -- calls `CreateRealmAsync`, `CreateUserAsync`, `AssignRealmRoleAsync`
  in order and returns 201 with correct body
- Unit test: if `CreateUserAsync` fails -> `DeleteUserAsync` is called as cleanup and handler
  returns a failure result

---

### B7 — Staff–Keycloak user sync

Update the three staff handler use cases to keep Keycloak users in sync.

**`CreateStaffMemberHandler`:**
1. Build the `StaffMember` entity and save to DB (get the entity ID)
2. Call `IKeycloakAdminService.CreateUserAsync(tenantId, email, firstName, lastName, role)`
3. Call `IKeycloakAdminService.AssignRealmRoleAsync(tenantId, keycloakUserId, mappedRole)`
   where `mappedRole` maps `StaffRole` -> `"manager"` or `"staff_member"`
4. Set `member.KeycloakUserId = keycloakUserId` and save again
5. If Keycloak fails: delete the DB record, return `OneOf` error result (map to 502 in endpoint)

**`UpdateStaffMemberHandler`:**
- If `Email`, `FirstName`, or `LastName` changed AND `member.KeycloakUserId != null`:
  call `IKeycloakAdminService.UpdateUserAsync(...)`
- Keycloak failure is non-fatal: log a warning, do not roll back the DB update
  (the DB is the source of truth; a background reconciliation job can fix Keycloak later)

**`DeactivateStaffMemberHandler`:**
- After setting `DeactivatedAtUtc` in DB: call `IKeycloakAdminService.DisableUserAsync(...)`
- Non-fatal on failure (same reasoning as above)

**`ReactivateStaffMemberHandler`:**
- After clearing `DeactivatedAtUtc` in DB: call `IKeycloakAdminService.EnableUserAsync(...)`
- Non-fatal on failure

Add a new `OneOf` error type `KeycloakError` (or reuse `Unprocessable`) to the CreateStaff result
union for the fatal Keycloak failure case on creation. Map to `502 Bad Gateway` in the endpoint.

**Tests:**
- Unit test: `CreateStaffMemberHandler` calls `CreateUserAsync` after saving to DB, sets
  `KeycloakUserId` on the entity, saves again
- Unit test: `CreateStaffMemberHandler` deletes DB record and returns error when Keycloak fails
- Unit test: `DeactivateStaffMemberHandler` calls `DisableUserAsync` after updating timestamps

---

### B8 — Runtime config endpoint

`GET /config` -- returns Keycloak connection details for the Angular app to initialize Keycloak at
startup. Public endpoint (no authentication required).

**Response:**
```json
{
  "keycloakUrl": "http://localhost:8080",
  "keycloakRealm": "00000000-0000-0000-0000-000000000001",
  "keycloakClientId": "chairly-frontend"
}
```

Read values from `IConfiguration["Keycloak:Url"]`, `["Keycloak:Realm"]`, `["Keycloak:ClientId"]`.

**Slice location:** `Chairly.Api/Features/Config/GetConfig/`

**Endpoint:** `GET /config` -- `.AllowAnonymous()`

**Tests:**
- Unit test: handler returns values from configuration

---

## Frontend Tasks

### F1 — Runtime config service + Keycloak-angular setup

**Install packages:**
```bash
npm install keycloak-js keycloak-angular
```
Check peer dependencies for Angular 21 compatibility and pin to the latest compatible versions.

**`AppConfigService` (`libs/shared/src/lib/data-access/app-config.service.ts`):**
```typescript
@Injectable({ providedIn: 'root' })
export class AppConfigService {
  private config: AppConfig | null = null;

  load(): Observable<AppConfig> {
    return this.http.get<AppConfig>('/api/config').pipe(
      tap(c => (this.config = c))
    );
  }

  get keycloakUrl(): string { return this.config!.keycloakUrl; }
  get keycloakRealm(): string { return this.config!.keycloakRealm; }
  get keycloakClientId(): string { return this.config!.keycloakClientId; }
}
```

**`AppConfig` model (`libs/shared/src/lib/data-access/models/app-config.model.ts`):**
```typescript
export interface AppConfig {
  keycloakUrl: string;
  keycloakRealm: string;
  keycloakClientId: string;
}
```

**`app.config.ts` -- `APP_INITIALIZER`:**
```typescript
{
  provide: APP_INITIALIZER,
  useFactory: (appConfig: AppConfigService, keycloak: KeycloakService) =>
    () => firstValueFrom(
      appConfig.load().pipe(
        switchMap(config =>
          from(keycloak.init({
            config: {
              url: config.keycloakUrl,
              realm: config.keycloakRealm,
              clientId: config.keycloakClientId,
            },
            initOptions: {
              onLoad: 'login-required',
              checkLoginIframe: false,
            },
          }))
        )
      )
    ),
  multi: true,
  deps: [AppConfigService, KeycloakService],
}
```

Also add `KeycloakService` to the providers array and add `provideHttpClient(withInterceptors([...]))`.

**Tests (Vitest):**
- Unit test: `AppConfigService.load()` calls `GET /api/config` and stores the result

---

### F2 — Bearer token interceptor + auth guard

**`AuthInterceptor` (`libs/shared/src/lib/data-access/auth.interceptor.ts`):**
A functional HTTP interceptor that retrieves the Keycloak token via `KeycloakService.getToken()`
and adds an `Authorization: Bearer {token}` header to every outgoing request, excluding requests
to `/api/config` (which is public).

Register in `app.config.ts`:
```typescript
provideHttpClient(withInterceptors([authInterceptor]))
```

**`AuthGuard` (`libs/shared/src/lib/data-access/auth.guard.ts`):**
A functional route guard (`CanActivateFn`) that checks `KeycloakService.isLoggedIn()`. If not
logged in, calls `KeycloakService.login()` and returns `false`. Use this guard on all
domain routes.

**`RoleGuard` (`libs/shared/src/lib/data-access/role.guard.ts`):**
A functional route guard factory that accepts a required role and checks
`KeycloakService.getUserRoles()`. Returns 403-equivalent (redirect to `/toegang-geweigerd`) if
the user does not have the required role.

**Route `toegang-geweigerd`:**
Add a simple "Toegang geweigerd" page component at `apps/chairly/src/app/` and a route
`{ path: 'toegang-geweigerd', component: AccessDeniedComponent }`.

Apply `authGuard` to all existing domain routes in the app's route config.

**Tests:**
- Unit test: `authInterceptor` adds `Authorization` header to non-config requests
- Unit test: `authInterceptor` does NOT add `Authorization` header to `/api/config`
- Unit test: `authGuard` redirects to login when not logged in

---

### F3 — Auth integration in nav + user info + logout

**`AuthStore` (`libs/shared/src/lib/data-access/auth.store.ts`):**
NgRx SignalStore that exposes:
```typescript
userFullName: Signal<string>    // firstName + lastName from Keycloak token
userRole: Signal<string>        // first matching role from token
isOwner: Signal<boolean>
isManager: Signal<boolean>      // true for owner OR manager
```
Load user info from `KeycloakService.loadUserProfile()` on initialization.

**Nav component updates (`apps/chairly/src/app/` -- existing nav/shell component):**
- Display the logged-in user's name (from `AuthStore.userFullName`)
- Add a "Uitloggen" button that calls `KeycloakService.logout()`
- Hide menu items that require a higher role (e.g., hide "Instellingen" for `staff_member`,
  hide "Facturatie" for `staff_member`)
  Use `AuthStore.isOwner` / `AuthStore.isManager` signals in the template with `@if`

**Role-based visibility convention:**
Do NOT use `RoleGuard` for hiding UI elements -- use `AuthStore` signals in templates only.
`RoleGuard` is for route-level protection only.

**E2E scenarios (Playwright):**
- Login flow: app redirects to Keycloak login, user logs in, lands on dashboard
- Logout: clicking "Uitloggen" redirects to Keycloak logout and back to Keycloak login page
- Role visibility: log in as `staff_member`, verify "Facturatie" nav item is absent
- Unauthorized route: manually navigate to a manager-only route as `staff_member`,
  verify redirect to `/toegang-geweigerd`

**Tests (Vitest):**
- Unit test: `AuthStore` computes `isManager` correctly for `owner` and `manager` roles

---

## Acceptance Criteria

- [ ] Keycloak container starts via `dotnet run` in AppHost and is accessible at the URL shown in
  the Aspire dashboard
- [ ] `GET /config` returns correct Keycloak URL, realm, and client ID
- [ ] Angular app redirects to Keycloak login on first visit (no slug entry required)
- [ ] After login, the Angular app loads and all API calls include a valid Bearer token
- [ ] Unauthenticated API requests return 401
- [ ] `staff_member` role receives 403 when calling Manager-only endpoints
- [ ] Creating a staff member also creates a Keycloak user in the correct realm
- [ ] Deactivating a staff member disables the corresponding Keycloak user
- [ ] `POST /tenants` creates a new realm and owner user in Keycloak
- [ ] Logout via the UI ends the Keycloak session
- [ ] All backend quality checks pass (`dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`)
- [ ] All frontend quality checks pass (`nx lint`, `nx test`, `nx build`)
- [ ] Playwright e2e tests for login, logout, and role visibility pass

## Out of Scope

- Self-service tenant onboarding UI (future: automated provisioning portal)
- MFA configuration in Keycloak
- Social login providers
- Password reset UI (Keycloak built-in account UI handles this)
- Per-StaffMember endpoint scoping (e.g., a staff member can only see their own bookings) --
  that is a future data-access-level concern
- Production Keycloak deployment and hardening
- `keycloak-angular` SSR compatibility (Chairly is a SPA, no SSR)
