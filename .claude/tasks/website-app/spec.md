# Website App

## Overview

Chairly needs a public-facing marketing website where prospective salon owners can learn about the platform, request a demo, or sign up for a new account. This is a **separate Angular application** (`chairly-website`) in the Nx workspace, distinct from the backoffice app. It connects to a **dedicated database container** (not the per-tenant databases used by the backoffice). A new **Onboarding** bounded context in the backend handles demo requests and sign-up requests. Automatic environment provisioning is out of scope for this iteration -- sign-up requests are stored and handled manually by an admin.

## Domain Context

- Bounded context: **Onboarding** (new)
- Key entities involved: **DemoRequest** (new), **SignUpRequest** (new)
- Ubiquitous language:
  - **DemoRequest** -- a prospective salon owner's request to see a live demonstration of Chairly
  - **SignUpRequest** -- a prospective salon owner's request to create a new Chairly environment (tenant)
  - **Tenant** -- a single salon location; provisioning a tenant is out of scope for this spec

### Entities

**`DemoRequest`** (Aggregate Root)

| Field | Type | Notes |
|---|---|---|
| `Id` | Guid | PK |
| `ContactName` | string | Required, max 200 |
| `SalonName` | string | Required, max 200 |
| `Email` | string | Required, max 256, valid email |
| `PhoneNumber` | string? | Optional, max 50 |
| `Message` | string? | Optional, max 2000 |
| `CreatedAtUtc` | DateTimeOffset | Required |
| `CreatedBy` | Guid? | Nullable (anonymous submission) |
| `ReviewedAtUtc` | DateTimeOffset? | Set when admin reviews the request |
| `ReviewedBy` | Guid? | Admin who reviewed |

**`SignUpRequest`** (Aggregate Root)

| Field | Type | Notes |
|---|---|---|
| `Id` | Guid | PK |
| `SalonName` | string | Required, max 200 |
| `OwnerFirstName` | string | Required, max 100 |
| `OwnerLastName` | string | Required, max 100 |
| `Email` | string | Required, max 256, valid email |
| `PhoneNumber` | string? | Optional, max 50 |
| `CreatedAtUtc` | DateTimeOffset | Required |
| `CreatedBy` | Guid? | Nullable (anonymous submission) |
| `ProvisionedAtUtc` | DateTimeOffset? | Set when environment is provisioned (future) |
| `ProvisionedBy` | Guid? | Admin who provisioned |
| `RejectedAtUtc` | DateTimeOffset? | Set if request is rejected |
| `RejectedBy` | Guid? | Admin who rejected |
| `RejectionReason` | string? | Optional, max 1000 |

### Business Rules

- No authentication required for submitting demo requests or sign-up requests (public endpoints)
- Email uniqueness for `SignUpRequest` uses a silent-success pattern: if a pending sign-up request with the same email already exists, the endpoint still returns `201 Created` with a synthetic response (never reveals whether the email is already in use). No duplicate record is created.
- `DemoRequest` has no uniqueness constraint (same person can request multiple demos)
- Both entities have no `TenantId` -- they live in the website database, not tenant databases
- A notification email is sent to a configured admin address when a demo request or sign-up request is submitted
- Derived statuses:
  - **DemoRequest**: Pending (only `CreatedAtUtc`), Reviewed (`ReviewedAtUtc` set)
  - **SignUpRequest**: Pending (only `CreatedAtUtc`), Provisioned (`ProvisionedAtUtc` set), Rejected (`RejectedAtUtc` set)

---

## Infrastructure Tasks

### I1 — Website database container in Aspire AppHost

Add a second PostgreSQL database resource to the Aspire AppHost for the website app. This database is separate from the per-tenant `ChairlyDb`.

**Files to modify:**
- `src/backend/Chairly.AppHost/Program.cs`

**Changes:**
- Add a new database resource on the existing `postgres` server: `.AddDatabase("WebsiteDb")`
- Pass the `WebsiteDb` connection string to the API project via `.WithReference(websiteDb)`

**DI registration (in `Chairly.Api/Program.cs`):**
- Register a new `WebsiteDbContext` alongside the existing `ChairlyDbContext`:
  ```
  builder.Services.AddDbContext<WebsiteDbContext>(options =>
      options.UseNpgsql(builder.Configuration.GetConnectionString("WebsiteDb")));
  ```
- Add startup migration logic for `WebsiteDbContext` using the same advisory-lock pattern as `ChairlyDbContext` (use a different lock key, e.g. `1_000_000_002`)

**Health check:** The existing Aspire health check infrastructure covers AddDatabase resources automatically.

**Test cases:**
- Verify `WebsiteDbContext` can be resolved from DI
- Verify migrations run on startup for the website database

---

### I2 — WebsiteDbContext and EF configuration

Create a new `WebsiteDbContext` in the Infrastructure project for the website database.

**Files to create:**
- `src/backend/Chairly.Infrastructure/Persistence/WebsiteDbContext.cs`

**WebsiteDbContext:**
```csharp
public class WebsiteDbContext(DbContextOptions<WebsiteDbContext> options) : DbContext(options)
{
    public DbSet<DemoRequest> DemoRequests => Set<DemoRequest>();
    public DbSet<SignUpRequest> SignUpRequests => Set<SignUpRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(WebsiteDbContext).Assembly,
            type => type.Namespace?.Contains("Website", StringComparison.Ordinal) == true);
        base.OnModelCreating(modelBuilder);
    }
}
```

Note: The `ApplyConfigurationsFromAssembly` call uses a namespace filter so that website-specific configurations do not get applied to `ChairlyDbContext` and vice versa.

**EF configurations (in `src/backend/Chairly.Infrastructure/Persistence/Configurations/Website/`):**

**`DemoRequestConfiguration.cs`:**
- Table: `DemoRequests`
- `ContactName` max 200, required
- `SalonName` max 200, required
- `Email` max 256, required
- `PhoneNumber` max 50
- `Message` max 2000
- Index on `CreatedAtUtc` (for listing in chronological order)

**`SignUpRequestConfiguration.cs`:**
- Table: `SignUpRequests`
- `SalonName` max 200, required
- `OwnerFirstName` max 100, required
- `OwnerLastName` max 100, required
- `Email` max 256, required
- `PhoneNumber` max 50
- `RejectionReason` max 1000
- Index on `Email` (for duplicate checking)
- Index on `CreatedAtUtc` (for listing)

**Migration:** Create a migration named `InitialWebsiteSchema` using:
```
dotnet ef migrations add InitialWebsiteSchema --context WebsiteDbContext --output-dir Migrations/Website
```
Migration must be idempotent: `CREATE TABLE IF NOT EXISTS`, `CREATE INDEX IF NOT EXISTS`.

**Design context factory:** Create `WebsiteDbContextDesignTimeFactory.cs` in the Infrastructure project so `dotnet ef` can instantiate `WebsiteDbContext` at design time (for migration generation). Follow the same pattern as any existing design-time factory for `ChairlyDbContext`.

**Test cases:**
- Verify `DemoRequest` entity has all expected properties
- Verify `SignUpRequest` entity has all expected properties
- Verify EF configurations create correct table schemas

---

## Backend Tasks

### B1 -- DemoRequest and SignUpRequest domain entities

Create the domain entities in `Chairly.Domain/Entities/`.

**`DemoRequest.cs`:**
```csharp
public class DemoRequest
{
    public Guid Id { get; set; }
    public string ContactName { get; set; } = string.Empty;
    public string SalonName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Message { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? ReviewedAtUtc { get; set; }
    public Guid? ReviewedBy { get; set; }
}
```

**`SignUpRequest.cs`:**
```csharp
public class SignUpRequest
{
    public Guid Id { get; set; }
    public string SalonName { get; set; } = string.Empty;
    public string OwnerFirstName { get; set; } = string.Empty;
    public string OwnerLastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? ProvisionedAtUtc { get; set; }
    public Guid? ProvisionedBy { get; set; }
    public DateTimeOffset? RejectedAtUtc { get; set; }
    public Guid? RejectedBy { get; set; }
    public string? RejectionReason { get; set; }
}
```

Note: These entities have no `TenantId` -- they are platform-level, not tenant-scoped.

**Test cases:**
- Verify all properties exist with correct types
- Verify no `TenantId` property on either entity

---

### B2 -- Submit demo request endpoint

**Slice:** `Chairly.Api/Features/Onboarding/SubmitDemoRequest/`

**Route:** `POST /api/onboarding/demo-requests`

**Command:** `SubmitDemoRequestCommand`
```csharp
internal sealed class SubmitDemoRequestCommand : IRequest<OneOf<SubmitDemoRequestResponse, Unprocessable>>
{
    [Required] [MaxLength(200)]
    public string ContactName { get; set; } = string.Empty;

    [Required] [MaxLength(200)]
    public string SalonName { get; set; } = string.Empty;

    [Required] [EmailAddress] [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? PhoneNumber { get; set; }

    [MaxLength(2000)]
    public string? Message { get; set; }
}
```

**Response DTO (shared in `Chairly.Api/Features/Onboarding/SubmitDemoRequestResponse.cs`):**
```json
{
  "id": "guid",
  "contactName": "string",
  "salonName": "string",
  "email": "string",
  "createdAtUtc": "datetimeoffset"
}
```

**Handler logic:**
1. Create a new `DemoRequest` entity with `Id = Guid.NewGuid()`, `CreatedAtUtc = DateTimeOffset.UtcNow`, `CreatedBy = null` (anonymous submission)
2. Save to `WebsiteDbContext`
3. Send notification email to configured admin address (use existing `IEmailSender` infrastructure) with subject "Nieuwe demo-aanvraag: {SalonName}" and body containing all submitted fields
4. Return `201 Created` with the response

**Endpoint:** `SubmitDemoRequestEndpoint` -- maps POST route, invokes mediator, returns `Results.Created(...)`. Must have `.AllowAnonymous()` (public endpoint).

**Validator:** `SubmitDemoRequestValidator`
- `ContactName`: required, max 200
- `SalonName`: required, max 200
- `Email`: required, valid email format, max 256
- `PhoneNumber`: if provided, max 50
- `Message`: if provided, max 2000

**Test cases:**
- Happy path returns 201 with created demo request
- Returns 422 when required fields are missing
- Returns 422 when email format is invalid
- Notification email is sent (verify via mock/spy)

---

### B3 -- Submit sign-up request endpoint

**Slice:** `Chairly.Api/Features/Onboarding/SubmitSignUpRequest/`

**Route:** `POST /api/onboarding/sign-up-requests`

**Command:** `SubmitSignUpRequestCommand`
```csharp
internal sealed class SubmitSignUpRequestCommand : IRequest<SubmitSignUpRequestResponse>
{
    [Required] [MaxLength(200)]
    public string SalonName { get; set; } = string.Empty;

    [Required] [MaxLength(100)]
    public string OwnerFirstName { get; set; } = string.Empty;

    [Required] [MaxLength(100)]
    public string OwnerLastName { get; set; } = string.Empty;

    [Required] [EmailAddress] [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? PhoneNumber { get; set; }
}
```

**Response DTO (shared in `Chairly.Api/Features/Onboarding/SubmitSignUpRequestResponse.cs`):**
```json
{
  "id": "guid",
  "salonName": "string",
  "ownerFirstName": "string",
  "ownerLastName": "string",
  "email": "string",
  "createdAtUtc": "datetimeoffset"
}
```

**Handler logic:**
1. Check if a pending `SignUpRequest` already exists with the same email (where `ProvisionedAtUtc` is null AND `RejectedAtUtc` is null).
2. If a pending request exists: return a synthetic `201 Created` response with a new `Guid` as `Id` and the current timestamp as `CreatedAtUtc`. Do NOT create a duplicate record and do NOT send a notification email. This prevents information leakage about existing sign-ups.
3. If no pending request exists: create a new `SignUpRequest` entity with `Id = Guid.NewGuid()`, `CreatedAtUtc = DateTimeOffset.UtcNow`, `CreatedBy = null`
4. Save to `WebsiteDbContext`
5. Send notification email to configured admin address with subject "Nieuwe aanmelding: {SalonName}" and body containing all submitted fields
6. Return `201 Created` with the response

**Endpoint:** `SubmitSignUpRequestEndpoint` -- maps POST route, invokes mediator, returns `Results.Created(...)`. Must have `.AllowAnonymous()`.

**Validator:** `SubmitSignUpRequestValidator`
- `SalonName`: required, max 200
- `OwnerFirstName`: required, max 100
- `OwnerLastName`: required, max 100
- `Email`: required, valid email format, max 256
- `PhoneNumber`: if provided, max 50

**Test cases:**
- Happy path returns 201 with created sign-up request
- Returns 422 when required fields are missing
- Returns 422 when email format is invalid
- Returns 201 (silent success) when a pending sign-up request with the same email already exists (no duplicate record created, no email sent)
- Allows re-submission (creates new record) if previous request was rejected or provisioned
- Notification email is sent on genuine new sign-up (verify via mock/spy)

---

### B4 -- Onboarding endpoint registration and admin email configuration

**Endpoint registration:**

Create `Chairly.Api/Features/Onboarding/OnboardingEndpoints.cs`:
```csharp
internal static class OnboardingEndpoints
{
    public static IEndpointRouteBuilder MapOnboardingEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapSubmitDemoRequest();
        app.MapSubmitSignUpRequest();
        return app;
    }
}
```

Register in `Program.cs`: `app.MapOnboardingEndpoints();`

**Admin email configuration:**

Add configuration section `Onboarding:AdminEmail` to `appsettings.json` (and `appsettings.Development.json`). The handlers read this value to determine where to send notification emails. In development, emails are captured by MailDev.

Example config:
```json
{
  "Onboarding": {
    "AdminEmail": "admin@chairly.nl"
  }
}
```

**Note on tenant middleware:** The onboarding endpoints are `AllowAnonymous` and do not carry a tenant header. The existing `TenantContextMiddleware` must not reject requests without a tenant header for these routes. Verify and if needed add a path-based exclusion (e.g., skip tenant resolution for `/api/onboarding/*` paths). The same pattern already exists for `/api/tenants`.

**Test cases:**
- Verify both onboarding endpoints are reachable
- Verify requests without tenant header succeed for onboarding routes

---

## Frontend Tasks

### F1 -- Nx app scaffold and workspace configuration for chairly-website

Create the new Angular application in the Nx workspace.

**Generate the app:**
```bash
cd src/frontend/chairly
npx nx g @nx/angular:application chairly-website --directory=apps/chairly-website --prefix=chairly-web --style=scss --routing --standalone
```

**Project structure:**
```
apps/chairly-website/
  src/
    app/
      app.ts
      app.html
      app.scss
      app.routes.ts
      app.config.ts
    main.ts
    index.html
    tailwind.css
    styles.scss
  project.json
  tsconfig.app.json
  tsconfig.spec.json
```

**Configuration:**
- `project.json`: configure build/serve/lint targets following the same pattern as the existing `chairly` app. Serve on a different port (e.g., 4300) to avoid conflicts.
- `tailwind.css`: import Tailwind CSS with `@import 'tailwindcss'` and appropriate `@source` directives pointing to `apps/chairly-website/src/` and `libs/website/src/`
- `styles.scss`: SCSS global styles (kept separate from `tailwind.css`)
- `postcss.config.json`: PostCSS config (JSON format) for the Angular builder, same setup as the backoffice app
- `index.html`: set `<html lang="nl">`, page title "Chairly - Salonsoftware"
- `app.config.ts`: register Dutch locale (`registerLocaleData(localeNl)`), provide `LOCALE_ID: 'nl-NL'`, `DEFAULT_CURRENCY_CODE: 'EUR'`. No Keycloak/auth setup needed (public site).
- Proxy config `proxy.conf.json`: proxy `/api` requests to the backend (same pattern as the backoffice app)

**E2E app:**
```bash
npx nx g @nx/playwright:configuration --project=chairly-website-e2e
```
Or create `apps/chairly-website-e2e/` manually following the `chairly-e2e` pattern.

**tsconfig.base.json update:** Add a path alias for the website library:
```json
"@org/website-lib": ["libs/website/src/index.ts"]
```

**Sheriff config update (`sheriff.config.ts`):**
- Add tagging for `libs/website/src/lib` with layers (same pattern as `libs/chairly/src/lib`)
- Allow `root` to depend on `['website-lib', 'shared']`
- Website domains follow the same layer rules as chairly-lib domains

**Test cases:**
- `npx nx build chairly-website` succeeds
- `npx nx serve chairly-website` starts on configured port
- `npx nx lint chairly-website` passes

---

### F2 -- Website library scaffold (libs/website)

Create the website-specific library in the Nx workspace.

**Generate the library:**
```bash
cd src/frontend/chairly
npx nx g @nx/angular:library website --directory=libs/website --prefix=chairly-web --style=scss --standalone
```

**Directory structure:**
```
libs/website/
  src/
    index.ts
    lib/
      onboarding/
        models/
          demo-request.model.ts
          sign-up-request.model.ts
          index.ts
        data-access/
          onboarding-api.service.ts
          index.ts
        feature/
          landing-page/
            landing-page.component.ts
            landing-page.component.html
            landing-page.component.scss
            landing-page.component.spec.ts
          demo-request-page/
            demo-request-page.component.ts
            demo-request-page.component.html
            demo-request-page.component.scss
            demo-request-page.component.spec.ts
          sign-up-page/
            sign-up-page.component.ts
            sign-up-page.component.html
            sign-up-page.component.scss
            sign-up-page.component.spec.ts
          confirmation-page/
            confirmation-page.component.ts
            confirmation-page.component.html
            confirmation-page.component.scss
            confirmation-page.component.spec.ts
        ui/
          header/
            header.component.ts
            header.component.html
            header.component.scss
            header.component.spec.ts
          footer/
            footer.component.ts
            footer.component.html
            footer.component.scss
            footer.component.spec.ts
          hero-section/
            hero-section.component.ts
            hero-section.component.html
            hero-section.component.scss
            hero-section.component.spec.ts
          feature-card/
            feature-card.component.ts
            feature-card.component.html
            feature-card.component.scss
            feature-card.component.spec.ts
        onboarding.routes.ts
```

**Barrel exports (`libs/website/src/index.ts`):**
- Export `onboardingRoutes` from `onboarding.routes.ts`

**Test cases:**
- Library builds without errors
- Lint passes

---

### F3 -- Onboarding models and API service

**Models (`libs/website/src/lib/onboarding/models/`):**

**`demo-request.model.ts`:**
```typescript
export interface SubmitDemoRequestPayload {
  contactName: string;
  salonName: string;
  email: string;
  phoneNumber: string | null;
  message: string | null;
}

export interface DemoRequestResponse {
  id: string;
  contactName: string;
  salonName: string;
  email: string;
  createdAtUtc: string;
}
```

**`sign-up-request.model.ts`:**
```typescript
export interface SubmitSignUpRequestPayload {
  salonName: string;
  ownerFirstName: string;
  ownerLastName: string;
  email: string;
  phoneNumber: string | null;
}

export interface SignUpRequestResponse {
  id: string;
  salonName: string;
  ownerFirstName: string;
  ownerLastName: string;
  email: string;
  createdAtUtc: string;
}
```

**Barrel (`models/index.ts`):** export all interfaces.

**API service (`data-access/onboarding-api.service.ts`):**
```typescript
@Injectable({ providedIn: 'root' })
export class OnboardingApiService {
  private readonly http = inject(HttpClient);

  submitDemoRequest(payload: SubmitDemoRequestPayload): Observable<DemoRequestResponse> {
    return this.http.post<DemoRequestResponse>('/api/onboarding/demo-requests', payload);
  }

  submitSignUpRequest(payload: SubmitSignUpRequestPayload): Observable<SignUpRequestResponse> {
    return this.http.post<SignUpRequestResponse>('/api/onboarding/sign-up-requests', payload);
  }
}
```

**Barrel (`data-access/index.ts`):** export `OnboardingApiService`.

**Test cases:**
- `OnboardingApiService` calls correct endpoints with correct payloads (HttpClientTestingModule)

---

### F4 -- Landing page component

**Location:** `libs/website/src/lib/onboarding/feature/landing-page/`

**Presentational component:** `LandingPageComponent`

This is the main marketing page visitors see when they arrive at the website. It is purely presentational -- it does not inject any services or load data from an API. All content is static marketing copy composed from child UI components.

**Component details:**
- `ChangeDetectionStrategy.OnPush`, standalone
- Selector: `chairly-web-landing-page`
- `templateUrl: './landing-page.component.html'`
- No injected services -- static content only

**Template structure:**
- **Header** (via `<chairly-web-header />` component): Chairly logo/name, navigation links: "Home", "Demo aanvragen", "Aanmelden"
- **Hero section** (via `<chairly-web-hero-section />`):
  - Heading: "De salon software die voor u werkt"
  - Subheading: "Beheer uw boekingen, klanten en facturatie op een plek. Eenvoudig, snel en veilig."
  - CTA buttons: "Demo aanvragen" (links to `/demo-aanvragen`), "Nu aanmelden" (links to `/aanmelden`)
- **Features section**: grid of feature cards (via `<chairly-web-feature-card />`) highlighting key capabilities:
  - "Boekingen beheren" -- "Plan en beheer afspraken moeiteloos met onze slimme agenda."
  - "Klantenbeheer" -- "Houd klantgegevens en -historie bij op een centrale plek."
  - "Facturatie" -- "Maak en verstuur facturen automatisch na elke afspraak."
  - "Meldingen" -- "Automatische herinneringen per e-mail voor uw klanten."
- **Footer** (via `<chairly-web-footer />`): copyright, links

**Styling:**
- Tailwind CSS with proper `dark:` variants
- Responsive design (mobile-first)
- Professional, clean marketing layout

**Unit tests:**
- `should create`
- `should render hero section with heading`
- `should render feature cards`
- `should have navigation links to demo and sign-up pages`

---

### F5 -- Demo request page component

**Location:** `libs/website/src/lib/onboarding/feature/demo-request-page/`

**Smart component:** `DemoRequestPageComponent`

**Component details:**
- `ChangeDetectionStrategy.OnPush`, standalone
- Selector: `chairly-web-demo-request-page`
- `templateUrl: './demo-request-page.component.html'`
- Inject `OnboardingApiService`, `DestroyRef`, `Router`
- Signals: `isSubmitting = signal(false)`, `submitError = signal<string | null>(null)`
- Use `ReactiveFormsModule` with typed `FormGroup`
- Use `takeUntilDestroyed(destroyRef)` for subscription cleanup

**Form fields:**

| Field | Label (Dutch) | Input type | Required | Notes |
|---|---|---|---|---|
| contactName | Naam | text | Yes | max 200 |
| salonName | Salonnaam | text | Yes | max 200 |
| email | E-mailadres | email | Yes | max 256 |
| phoneNumber | Telefoonnummer | tel | No | max 50 |
| message | Bericht | textarea | No | max 2000, 4 rows |

**Template structure:**
- Header and footer (shared components)
- Page heading: "Demo aanvragen"
- Description: "Vul het formulier in en wij nemen zo snel mogelijk contact met u op voor een persoonlijke demo."
- Form with all fields in a card layout
- "Versturen" submit button; disabled while `isSubmitting()`
- Error message display when `submitError()` is set
- On successful submission, navigate to `/bevestiging` with query param `type=demo`

**Unit tests:**
- `should create`
- `should call OnboardingApiService.submitDemoRequest on form submit`
- `should disable submit button while submitting`
- `should navigate to confirmation page on success`
- `should display error message on failure`

---

### F6 -- Sign-up page component

**Location:** `libs/website/src/lib/onboarding/feature/sign-up-page/`

**Smart component:** `SignUpPageComponent`

**Component details:**
- `ChangeDetectionStrategy.OnPush`, standalone
- Selector: `chairly-web-sign-up-page`
- `templateUrl: './sign-up-page.component.html'`
- Inject `OnboardingApiService`, `DestroyRef`, `Router`
- Signals: `isSubmitting = signal(false)`, `submitError = signal<string | null>(null)`
- Use `ReactiveFormsModule` with typed `FormGroup`
- Use `takeUntilDestroyed(destroyRef)` for subscription cleanup

**Form fields:**

| Field | Label (Dutch) | Input type | Required | Notes |
|---|---|---|---|---|
| salonName | Salonnaam | text | Yes | max 200 |
| ownerFirstName | Voornaam | text | Yes | max 100 |
| ownerLastName | Achternaam | text | Yes | max 100 |
| email | E-mailadres | email | Yes | max 256 |
| phoneNumber | Telefoonnummer | tel | No | max 50 |

**Template structure:**
- Header and footer (shared components)
- Page heading: "Aanmelden"
- Description: "Maak een nieuw Chairly-account aan voor uw salon. Na goedkeuring ontvangt u een e-mail met uw inloggegevens."
- Form with all fields in a card layout
- "Aanmelden" submit button; disabled while `isSubmitting()`
- Error message display when `submitError()` is set (generic server error only; duplicate emails return silent success from backend)
- On successful submission, navigate to `/bevestiging` with query param `type=aanmelding`

**Unit tests:**
- `should create`
- `should call OnboardingApiService.submitSignUpRequest on form submit`
- `should disable submit button while submitting`
- `should navigate to confirmation page on success`
- `should display generic error message on server failure`

---

### F7 -- Confirmation page component

**Location:** `libs/website/src/lib/onboarding/feature/confirmation-page/`

**Smart component:** `ConfirmationPageComponent`

**Component details:**
- `ChangeDetectionStrategy.OnPush`, standalone
- Selector: `chairly-web-confirmation-page`
- `templateUrl: './confirmation-page.component.html'`
- Reads `type` query parameter to show context-specific message

**Template structure:**
- Header and footer (shared components)
- Success icon/illustration
- **For `type=demo`:**
  - Heading: "Bedankt voor uw aanvraag!"
  - Message: "Wij hebben uw demo-aanvraag ontvangen en nemen zo snel mogelijk contact met u op."
- **For `type=aanmelding`:**
  - Heading: "Bedankt voor uw aanmelding!"
  - Message: "Wij verwerken uw aanvraag zo snel mogelijk. U ontvangt een e-mail zodra uw omgeving klaar is."
- "Terug naar home" link/button (navigates to `/`)

**Unit tests:**
- `should create`
- `should display demo confirmation when type=demo`
- `should display sign-up confirmation when type=aanmelding`
- `should have a link back to home page`

---

### F8 -- Presentational UI components (header, footer, hero, feature card)

**Location:** `libs/website/src/lib/onboarding/ui/`

**Header component (`ui/header/`):**
- Selector: `chairly-web-header`
- Presentational, `OnPush`, standalone
- Navigation bar with Chairly logo/name and links: "Home" (`/`), "Demo aanvragen" (`/demo-aanvragen`), "Aanmelden" (`/aanmelden`)
- Responsive: hamburger menu on mobile
- Tailwind styling with `dark:` variants

**Footer component (`ui/footer/`):**
- Selector: `chairly-web-footer`
- Presentational, `OnPush`, standalone
- Copyright text: "(c) 2026 Chairly. Alle rechten voorbehouden."
- Minimal footer with links (optional: "Privacy", "Voorwaarden" -- can be placeholder links for now)

**Hero section component (`ui/hero-section/`):**
- Selector: `chairly-web-hero-section`
- Presentational, `OnPush`, standalone
- Inputs:
  - `heading = input.required<string>()`
  - `subheading = input.required<string>()`
  - `primaryCtaLabel = input.required<string>()`
  - `primaryCtaLink = input.required<string>()`
  - `secondaryCtaLabel = input.required<string>()`
  - `secondaryCtaLink = input.required<string>()`
- Uses `RouterLink` for CTA navigation
- Large, visually prominent section with background gradient/color

**Feature card component (`ui/feature-card/`):**
- Selector: `chairly-web-feature-card`
- Presentational, `OnPush`, standalone
- Inputs: `title = input<string>()`, `description = input<string>()`
- Card layout with icon placeholder, title, and description
- Tailwind styling with `dark:` variants

**Unit tests (for each component):**
- `should create`
- Verify rendered content matches inputs

---

### F9 -- Routing configuration

**Onboarding routes (`libs/website/src/lib/onboarding/onboarding.routes.ts`):**
```typescript
export const onboardingRoutes: Routes = [
  { path: '', loadComponent: () => import('./feature/landing-page/landing-page.component').then(m => m.LandingPageComponent) },
  { path: 'demo-aanvragen', loadComponent: () => import('./feature/demo-request-page/demo-request-page.component').then(m => m.DemoRequestPageComponent) },
  { path: 'aanmelden', loadComponent: () => import('./feature/sign-up-page/sign-up-page.component').then(m => m.SignUpPageComponent) },
  { path: 'bevestiging', loadComponent: () => import('./feature/confirmation-page/confirmation-page.component').then(m => m.ConfirmationPageComponent) },
];
```

**App routes (`apps/chairly-website/src/app/app.routes.ts`):**
```typescript
export const appRoutes: Route[] = [
  {
    path: '',
    loadChildren: () => import('@org/website-lib').then(m => m.onboardingRoutes),
  },
];
```

No authentication guards -- all routes are public.

**Test cases:**
- All routes are reachable
- Navigation between pages works

---

### F10 -- Playwright e2e tests for website app

**Location:** `apps/chairly-website-e2e/src/`

**Test infrastructure:** Tests use `page.route()` to intercept API calls and return mock responses. This avoids requiring a running backend and WebsiteDb during e2e test execution. Each test file sets up route interception for the relevant `/api/onboarding/*` endpoints before navigating to the page. The landing page and confirmation page tests need no API mocking (purely static content). The demo request and sign-up tests mock `POST` responses to simulate successful submissions.

**Test files:**

**`landing-page.spec.ts`:**
- Navigate to `/`
- Verify hero section heading "De salon software die voor u werkt" is visible
- Verify feature cards are rendered (at least 4)
- Click "Demo aanvragen" CTA and verify navigation to `/demo-aanvragen`
- Click "Aanmelden" CTA and verify navigation to `/aanmelden`
- Verify header navigation links work
- Verify footer is rendered with copyright text

**`demo-request.spec.ts`:**
- Navigate to `/demo-aanvragen`
- Verify page heading "Demo aanvragen" is visible
- Fill in all required fields (contactName, salonName, email)
- Click "Versturen" and verify navigation to `/bevestiging?type=demo`
- Verify confirmation page shows "Bedankt voor uw aanvraag!"
- Test form validation: submit empty form, verify required field errors

**`sign-up.spec.ts`:**
- Navigate to `/aanmelden`
- Verify page heading "Aanmelden" is visible
- Fill in all required fields (salonName, ownerFirstName, ownerLastName, email)
- Click "Aanmelden" button and verify navigation to `/bevestiging?type=aanmelding`
- Verify confirmation page shows "Bedankt voor uw aanmelding!"
- Test form validation: submit empty form, verify required field errors

---

## Acceptance Criteria

- [ ] `DemoRequest` and `SignUpRequest` entities exist in `Chairly.Domain/Entities/`
- [ ] `WebsiteDbContext` exists with DbSets for both entities
- [ ] EF configurations create correct schemas with proper constraints and indexes
- [ ] Migration is idempotent (`CREATE TABLE IF NOT EXISTS`, `CREATE INDEX IF NOT EXISTS`)
- [ ] Website database is a separate PostgreSQL database in Aspire AppHost
- [ ] Startup migrations run for `WebsiteDbContext` with advisory lock
- [ ] `POST /api/onboarding/demo-requests` creates a demo request and sends admin notification email
- [ ] `POST /api/onboarding/sign-up-requests` creates a sign-up request and sends admin notification email
- [ ] Duplicate pending sign-up email returns silent 201 (no duplicate record created, no information leakage)
- [ ] Both onboarding endpoints work without authentication (AllowAnonymous)
- [ ] Onboarding endpoints work without tenant header
- [ ] `chairly-website` Angular app builds and serves
- [ ] Landing page renders with hero section, feature cards, navigation
- [ ] Demo request form submits and navigates to confirmation page
- [ ] Sign-up form submits and navigates to confirmation page
- [ ] Confirmation page shows context-specific messages (demo vs. aanmelding)
- [ ] All user-facing text is Dutch
- [ ] All backend quality checks pass (`dotnet build`, `dotnet test`, `dotnet format`)
- [ ] All frontend quality checks pass (`nx lint`, `nx format:check`, `nx test`, `nx build`)
- [ ] Playwright e2e tests pass for the website app

## Out of Scope

- Automatic environment provisioning / tenant deployment (future spec once hosting decisions are made)
- Admin panel for reviewing demo requests and sign-up requests
- Multi-language / i18n support (Dutch only for now)
- Custom domain per tenant (`<customer>.chairly.com` routing)
- SEO optimization / meta tags / Open Graph
- Cookie consent / privacy policy / terms of service content
- Contact form (separate from demo request)
- Pricing page
- Blog / content management
- Analytics / tracking integration
- Dark mode for the website (can be added later; focus on light theme first)
- Logo / brand assets design
