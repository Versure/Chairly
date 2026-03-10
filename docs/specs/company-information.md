# Company Information

## Overview

Owners need a central place to manage company information that is displayed on invoices. This includes company name, email, address, phone number, IBAN, VAT number, and payment period. A new `TenantSettings` entity in the Settings bounded context stores this data per tenant. A settings page in the frontend allows owners to view and update this information. Fixes GitHub issue #39.

## Domain Context

- Bounded context: Settings (new)
- Key entities involved: `TenantSettings` (new Aggregate Root)
- Ubiquitous language:
  - **TenantSettings** — configuration record for a tenant; exactly one per tenant
  - **Owner** — the only role allowed to update settings

### Entities

**`TenantSettings`** (Aggregate Root)

| Field | Type | Notes |
|---|---|---|
| `Id` | Guid | PK |
| `TenantId` | Guid | Unique — one settings record per tenant |
| `CompanyName` | string? | max 200 |
| `CompanyEmail` | string? | max 200 |
| `CompanyAddress` | string? | max 500 |
| `CompanyPhone` | string? | max 50 |
| `IbanNumber` | string? | max 34 (IBAN max length) |
| `VatNumber` | string? | max 50 |
| `PaymentPeriodDays` | int? | e.g. 14, 30; null means not configured |
| `CreatedAtUtc` | DateTimeOffset | Required |
| `CreatedBy` | Guid | Required |
| `UpdatedAtUtc` | DateTimeOffset? | Set on every update |
| `UpdatedBy` | Guid? | |

### Business Rules

- Exactly one `TenantSettings` record per tenant; auto-created on first GET if missing (with all nullable fields null).
- Owner only for PUT. GET is available to Owner and Manager.
- All fields are optional (nullable); an empty settings object is valid.

---

## Backend Tasks

### B1 — TenantSettings entity, EF configuration, and migration

**Domain — `Chairly.Domain/Entities/TenantSettings.cs`:**
```csharp
public class TenantSettings
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string? CompanyName { get; set; }
    public string? CompanyEmail { get; set; }
    public string? CompanyAddress { get; set; }
    public string? CompanyPhone { get; set; }
    public string? IbanNumber { get; set; }
    public string? VatNumber { get; set; }
    public int? PaymentPeriodDays { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid? UpdatedBy { get; set; }
}
```

**EF Configuration — `Chairly.Infrastructure/Configurations/TenantSettingsConfiguration.cs`:**
- Table: `TenantSettings`
- Unique index on `TenantId` — exactly one record per tenant
- `CompanyName` max 200, `CompanyEmail` max 200, `CompanyAddress` max 500, `CompanyPhone` max 50, `IbanNumber` max 34, `VatNumber` max 50
- Add `DbSet<TenantSettings> TenantSettings { get; set; }` to `ChairlyDbContext`

**Migration:** `AddTenantSettings`

---

### B2 — Get company information endpoint

**Slice:** `Chairly.Api/Features/Settings/GetCompanyInfo/`

**Route:** `GET /api/settings/company`

**Handler logic:**
1. Query `TenantSettings` by `TenantId`.
2. If no record exists, auto-create one with all nullable fields null: set `Id = NewGuid()`, `TenantId`, `CreatedAtUtc = UtcNow`, `CreatedBy = currentUserId`. Save to database.
3. Return `200 OK` with the settings record.

**Response DTO (shared in `Chairly.Api/Features/Settings/CompanyInfoResponse.cs`):**
```json
{
  "companyName": "string?",
  "companyEmail": "string?",
  "companyAddress": "string?",
  "companyPhone": "string?",
  "ibanNumber": "string?",
  "vatNumber": "string?",
  "paymentPeriodDays": "int?"
}
```

**Access:** Owner and Manager.

**Tests:**
- Returns auto-created empty settings when none exists
- Returns stored values when settings exist
- Returns 403 for StaffMember callers

---

### B3 — Update company information endpoint

**Slice:** `Chairly.Api/Features/Settings/UpdateCompanyInfo/`

**Route:** `PUT /api/settings/company`

**Request body:**
```json
{
  "companyName": "string?",
  "companyEmail": "string?",
  "companyAddress": "string?",
  "companyPhone": "string?",
  "ibanNumber": "string?",
  "vatNumber": "string?",
  "paymentPeriodDays": "int?"
}
```

**Handler logic:**
1. Load `TenantSettings` by `TenantId`. If missing, create a new record (same as B2).
2. Update all fields from the request (null values are allowed — clearing a field is valid).
3. Set `UpdatedAtUtc = UtcNow`, `UpdatedBy = currentUserId`.
4. Save and return `200 OK` with the updated `CompanyInfoResponse`.

**Access:** Owner only. Return `403` for Manager or StaffMember.

**Validation:**
- `CompanyEmail`: if provided, must be a valid email format (Data Annotations `[EmailAddress]`)
- `PaymentPeriodDays`: if provided, must be between 1 and 365
- `IbanNumber`: if provided, max 34 characters

**Tests:**
- Happy path returns 200 with updated values
- Accepts null values (clearing a previously set field)
- Returns 422 when email format is invalid
- Returns 422 when PaymentPeriodDays is out of range
- Returns 403 when Manager or StaffMember attempts update

---

## Frontend Tasks

### F1 — Settings domain setup, models, and API service

**Create new domain `settings` in `libs/chairly/src/lib/settings/`.**

**Models (`models/company-info.model.ts`):**
```typescript
export interface CompanyInfo {
  companyName: string | null;
  companyEmail: string | null;
  companyAddress: string | null;
  companyPhone: string | null;
  ibanNumber: string | null;
  vatNumber: string | null;
  paymentPeriodDays: number | null;
}

export interface UpdateCompanyInfoRequest {
  companyName: string | null;
  companyEmail: string | null;
  companyAddress: string | null;
  companyPhone: string | null;
  ibanNumber: string | null;
  vatNumber: string | null;
  paymentPeriodDays: number | null;
}
```

**Barrel (`models/index.ts`):** export both interfaces.

**API service (`data-access/settings-api.service.ts`):**
```typescript
@Injectable({ providedIn: 'root' })
export class SettingsApiService {
  getCompanyInfo(): Observable<CompanyInfo>
  updateCompanyInfo(request: UpdateCompanyInfoRequest): Observable<CompanyInfo>
}
```
Use `HttpClient` injected via `inject()`. Base URL: `/api/settings/company`.

**Barrel (`data-access/index.ts`):** export `SettingsApiService`.

**Settings routes (`settings.routes.ts` at domain root):**
```typescript
export const settingsRoutes: Routes = [
  {
    path: 'instellingen',
    loadComponent: () =>
      import('./feature/company-info-page/company-info-page.component').then(
        (m) => m.CompanyInfoPageComponent,
      ),
  },
];
```

**Register in app routes:** import `settingsRoutes` and add to the lazy-loaded routes in the main app routing.

**Sidebar nav:** add "Instellingen" link to the sidebar in `ShellComponent` template (`libs/shared/src/lib/ui/shell/shell.component.html`) — place it at the bottom of the nav list, above the theme toggle, pointing to `/instellingen`.

---

### F2 — Company info settings page

**Location:** `libs/chairly/src/lib/settings/feature/company-info-page/`

**Files:**
- `company-info-page.component.ts`
- `company-info-page.component.html`
- `company-info-page.component.spec.ts`

**Smart component:** `CompanyInfoPageComponent`

Loads current company info on init via `SettingsApiService.getCompanyInfo()`. Shows a form with all fields. On submit, calls `SettingsApiService.updateCompanyInfo()`.

**Component details:**
- `ChangeDetectionStrategy.OnPush`, standalone
- Inject `SettingsApiService` and `DestroyRef`
- Signals: `isLoading = signal(false)`, `isSaving = signal(false)`, `saveSuccess = signal(false)`, `saveError = signal<string | null>(null)`
- Use `ReactiveFormsModule` with a typed `FormGroup`

**Form fields:**
| Field | Label | Input type | Notes |
|---|---|---|---|
| companyName | Bedrijfsnaam | text | optional |
| companyEmail | E-mailadres | email | optional |
| companyAddress | Adres | textarea | optional |
| companyPhone | Telefoonnummer | tel | optional |
| ibanNumber | IBAN-nummer | text | optional |
| vatNumber | BTW-nummer | text | optional |
| paymentPeriodDays | Betaaltermijn (dagen) | number, min=1 | optional |

**Template structure:**
- Page heading: "Bedrijfsinformatie"
- Sub-heading / description: "Deze gegevens worden gebruikt op uw facturen."
- Loading indicator (`<chairly-loading-indicator message="Instellingen laden..." />`) while fetching
- Form with all fields in a card/section layout
- "Opslaan" submit button; disabled while `isSaving()`
- Success banner: "Instellingen opgeslagen" (auto-dismiss after 3 seconds via `setTimeout`)
- Error banner: display `saveError()` when set

**Unit tests:**
- `should create`
- `should load company info on init`
- `should call update service on form submit`
- `should show success message after save`

**Playwright e2e (in `apps/chairly-e2e/src/settings.spec.ts`):**
- Navigate to `/instellingen`
- Verify the page heading "Bedrijfsinformatie" is visible
- Fill in company name and email, click "Opslaan"
- Verify success banner appears

---

## Acceptance Criteria

- [ ] `TenantSettings` entity exists in `Chairly.Domain/Entities/TenantSettings.cs`
- [ ] EF configuration with unique index on `TenantId`
- [ ] `GET /api/settings/company` auto-creates empty settings if none exist
- [ ] `PUT /api/settings/company` updates all fields; Owner only
- [ ] All backend tests pass (happy path, 403 for non-owners, 422 for invalid email)
- [ ] Settings domain exists at `libs/chairly/src/lib/settings/`
- [ ] `SettingsApiService` with `getCompanyInfo()` and `updateCompanyInfo()` methods
- [ ] Company info form page at `/instellingen` with all required fields
- [ ] "Instellingen" sidebar link added
- [ ] All user-facing text is Dutch
- [ ] All backend quality checks pass (dotnet build, test, format)
- [ ] All frontend quality checks pass (lint, test, build)
- [ ] Playwright e2e tests pass

## Out of Scope

- VAT rate settings (separate spec — vat feature)
- Role-based visibility for the settings nav item (always visible for now)
- Logo upload
- Multiple tenant locations
- Email/IBAN format validation beyond basic length/format checks
