# VAT (BTW)

> **Status: Implemented** — Merged to main.

## Overview

VAT (BTW) must be tracked per service and per invoice line item. A default VAT rate is configured on a settings page; each individual service can override this default. When an invoice is generated, the VAT rate is captured at that moment and frozen on the invoice line item -- it is never updated when service or default VAT rates change later. Prices are entered inclusive of VAT (bijv. EUR 39,99 incl. 21% BTW). Fixes GitHub issue #37.

The codebase already has `VatPercentage` and `VatAmount` fields on `InvoiceLineItem` (added in migration `AddInvoiceVatAndManualLineItems`), and the `GenerateInvoiceHandler` hardcodes `DefaultVatPercentage = 21.00m`. This feature replaces the hardcoded default with a configurable `VatSettings` entity and adds an optional `VatRate` override per `Service`.

## Domain Context

- Bounded context: Services + Billing + Settings
- Key entities involved: `Service` (existing), `InvoiceLineItem` (existing), `VatSettings` (new)
- Ubiquitous language:
  - **VatRate** / **VatPercentage** -- percentage of VAT applied to a service price; e.g. 21 means 21%. The `InvoiceLineItem` entity uses `VatPercentage`; the `Service` entity will use `VatRate`. Both represent the same concept.
  - **DefaultVatRate** -- tenant-wide fallback VAT rate used when a service has no `VatRate` set
  - **PriceInclVat** -- the price entered by the user; includes VAT. `Service.Price` is always incl-VAT.
  - **VatAmount** -- the VAT portion: `PriceInclVat * VatPercentage / (100 + VatPercentage)`
  - **PriceExclVat** -- `PriceInclVat - VatAmount` (derived, not stored)

### Business Rules

- Prices are inclusive of VAT. The stored `Service.Price` is the incl-VAT price.
- Each `Service` has an optional `VatRate` (`decimal?`). When null, the default rate from `VatSettings.DefaultVatRate` is applied.
- Common VAT rates in the Netherlands: 0%, 9%, 21%. Users select from these options (not a free-entry field).
- When generating an invoice, for each line item: capture the effective `VatPercentage` as it is at that moment. Subsequent changes to the service's VAT rate do not affect existing invoices.
- `VatAmount = round(UnitPrice * VatPercentage / (100 + VatPercentage), 2)` -- note: this is the incl-VAT formula, extracting VAT from the gross price.
- `TotalAmount` on `Invoice` = `SubTotalAmount + TotalVatAmount` (existing calculation, no change needed).

### Existing state in the codebase

- `InvoiceLineItem` already has `VatPercentage` (decimal, precision 5,2) and `VatAmount` (decimal, precision 18,2). No new columns needed on `InvoiceLineItem`.
- `Invoice` already has `SubTotalAmount` and `TotalVatAmount`. No new columns needed on `Invoice`.
- `GenerateInvoiceHandler` hardcodes `DefaultVatPercentage = 21.00m` and calculates VAT as `totalPrice * 21 / 100` (percentage-on-top). This must be changed to resolve the rate from `VatSettings`/`Service.VatRate` and use the incl-VAT formula: `unitPrice * rate / (100 + rate)`.
- The frontend `InvoiceLineItem` model already has `vatPercentage` and `vatAmount`.
- The invoice detail page already displays a "BTW %" column and VAT totals.

---

## Backend Tasks

### B1 — VatSettings entity, EF configuration, and migration

Create a new `VatSettings` entity to store the tenant-wide default VAT rate.

**Domain -- `Chairly.Domain/Entities/VatSettings.cs`:**
```csharp
public class VatSettings
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public decimal DefaultVatRate { get; set; } = 21m;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid? UpdatedBy { get; set; }
}
```

**EF Configuration -- `Chairly.Infrastructure/Persistence/Configurations/VatSettingsConfiguration.cs`:**
- Table: `VatSettings`
- Unique index on `TenantId` -- exactly one record per tenant
- `DefaultVatRate` precision `(5, 2)` -- e.g. 21.00, 9.00, 0.00
- `CreatedBy` required, `UpdatedBy` nullable
- Add `DbSet<VatSettings> VatSettings { get; set; }` to `ChairlyDbContext`

**Migration:** `AddVatSettings` -- must be idempotent per CLAUDE.md rules:
- `CREATE TABLE IF NOT EXISTS "VatSettings"` via raw SQL
- `CREATE INDEX IF NOT EXISTS` for the unique tenant index

---

### B2 — Add VatRate to Service entity and migration

Add `VatRate` (nullable decimal) to the `Service` entity.

**Update `Chairly.Domain/Entities/Service.cs`:**
Add after the `Price` property:
```csharp
public decimal? VatRate { get; set; }
```

**Update `Chairly.Infrastructure/Persistence/Configurations/ServiceConfiguration.cs`:**
Add:
```csharp
builder.Property(s => s.VatRate).HasPrecision(5, 2).IsRequired(false);
```

**Migration:** `AddVatRateToService` -- must be idempotent:
- Use `DO $$ BEGIN IF NOT EXISTS ... THEN ALTER TABLE "Services" ADD COLUMN "VatRate" numeric(5,2); END IF; END $$;`

**Update `CreateServiceCommand.cs`:**
Add `public decimal? VatRate { get; set; }` field.
Validation: if not null, must be one of `0`, `9`, `21` (the valid Dutch VAT rates). Use a custom validation attribute or manual validation in the handler.

**Update `CreateServiceHandler.cs`:**
Set `service.VatRate = command.VatRate` when creating the entity.

**Update `UpdateServiceCommand.cs`:**
Add `public decimal? VatRate { get; set; }` field. Same validation.

**Update `UpdateServiceHandler.cs`:**
Set `service.VatRate = command.VatRate` when updating.

**Update `ServiceResponse.cs`:**
Add `decimal? VatRate` parameter to the record:
```csharp
internal sealed record ServiceResponse(
    ...,
    decimal? VatRate);
```

**Update all places that construct a `ServiceResponse`:**
- `CreateServiceHandler.ToResponse()` -- pass `service.VatRate`
- `UpdateServiceHandler` -- pass `service.VatRate`
- `GetServiceHandler` -- pass `service.VatRate`
- `GetServicesListHandler` -- pass `service.VatRate`

**Tests:**
- Creating a service with VatRate=21 stores it correctly
- Creating a service with null VatRate stores null
- Creating a service with VatRate=15 returns 422 (invalid rate)
- Updating a service updates VatRate correctly
- GetService returns VatRate in response

---

### B3 — Update GenerateInvoice to resolve VAT from VatSettings and Service

Update the `GenerateInvoiceHandler` to dynamically resolve the VAT rate per line item instead of hardcoding 21%.

**Update `Chairly.Api/Features/Billing/GenerateInvoice/GenerateInvoiceHandler.cs`:**

1. Remove the `private const decimal DefaultVatPercentage = 21.00m;` constant.
2. In the handler, load `VatSettings` for the tenant: `await db.VatSettings.FirstOrDefaultAsync(v => v.TenantId == tenantId)`.
3. If no `VatSettings` record exists, auto-create one with `DefaultVatRate = 21m` and persist it.
4. For each `BookingService`:
   a. Load the corresponding `Service` entity to get its `VatRate`.
   b. Resolve: `effectiveVatPercentage = service.VatRate ?? vatSettings.DefaultVatRate`
5. Update the VAT calculation to use the incl-VAT formula:
   - `vatAmount = Math.Round(unitPrice * effectiveVatPercentage / (100m + effectiveVatPercentage), 2, MidpointRounding.AwayFromZero)`
6. Set `lineItem.VatPercentage = effectiveVatPercentage`.

**Note:** The `BuildInvoiceAsync` method must become async-aware of service loading, or the service data must be pre-loaded before the LINQ projection.

**Tests:**
- GenerateInvoice captures VatPercentage=21 from service on line item when service has VatRate=21
- GenerateInvoice falls back to DefaultVatRate when service.VatRate is null
- VatAmount calculation is correct with incl-VAT formula: price 39.99, rate 21 -> vatAmount = round(39.99 * 21 / 121, 2) = 6.94
- GenerateInvoice auto-creates VatSettings when none exists

---

### B4 — VAT settings endpoints

**Slice:** `Chairly.Api/Features/Settings/GetVatSettings/` and `Chairly.Api/Features/Settings/UpdateVatSettings/`

Create a new `Settings` feature context within the API project.

**GET /api/settings/vat:**
- Query: `GetVatSettingsQuery` implementing `IRequest<VatSettingsResponse>`
- Handler: load `VatSettings` for tenant. If none exists, auto-create with `DefaultVatRate = 21` and persist.
- Return `200 OK` with `VatSettingsResponse`.
- Access: Owner and Manager.

**PUT /api/settings/vat:**
- Command: `UpdateVatSettingsCommand` with `public decimal DefaultVatRate { get; set; }`
- Validation: `DefaultVatRate` must be one of `0`, `9`, `21`.
- Handler: load `VatSettings` for tenant (auto-create if missing), update `DefaultVatRate`, set `UpdatedAtUtc` and `UpdatedBy`, save.
- Return `200 OK` with updated `VatSettingsResponse`.
- Access: Owner only.

**Response DTO -- `VatSettingsResponse.cs`:**
```csharp
internal sealed record VatSettingsResponse(decimal DefaultVatRate);
```

**Endpoint registration:** Create `Chairly.Api/Features/Settings/SettingsEndpoints.cs` with a `MapSettingsEndpoints()` extension method, register in `Program.cs`.

**Tests:**
- GET auto-creates with DefaultVatRate=21 when no settings exist
- GET returns existing settings
- PUT updates DefaultVatRate to 9 successfully
- PUT returns 422 for invalid rate (e.g. 15)
- PUT returns 403 for non-Owner (if role checks are implemented)

---

## Frontend Tasks

### F1 — Update service models and form with VatRate

**Update service models** to include `VatRate`:

In `libs/chairly/src/lib/services/models/service.models.ts`:
- Add `vatRate: number | null` to `ServiceResponse` interface
- Add `vatRate: number | null` to `CreateServiceRequest` interface
- Add `vatRate: number | null` to `UpdateServiceRequest` interface

**Update the service form dialog component** (in `libs/chairly/src/lib/services/ui/service-form-dialog/`):

**Component TS (`service-form-dialog.component.ts`):**
- Add `vatRate` field to the form `FormGroup`:
  ```typescript
  vatRate: new FormControl<number>(21, {
    nonNullable: true,
    validators: [Validators.required],
  }),
  ```
- Update the `open()` method to set `vatRate` from the service being edited (`svc.vatRate ?? 21`) or default to `21` for new services.
- Update the `onSave()` method to include `vatRate` in the emitted request object.

**Template (`service-form-dialog.component.html`):**
- Add a `<select>` dropdown between the Price field and the Category field:
  - Label: "BTW-tarief"
  - Options: `0%`, `9%`, `21%` with values `0`, `9`, `21`
  - `formControlName="vatRate"`
  - Required (always a selection)

**Update `ServiceListPageComponent` or other parent components** that create/update services via the API to pass `vatRate` from the form to the API service.

**Update the service table component** if it displays service details -- consider showing the VAT rate in the service list table.

---

### F2 — Settings domain setup and VAT settings page

**Location:** `libs/chairly/src/lib/settings/` (new domain)

Since the settings domain does not yet exist in the frontend, create it following the standard domain structure:
```
libs/chairly/src/lib/settings/
  models/
    index.ts
    vat-settings.models.ts
  data-access/
    index.ts
    settings-api.service.ts
  feature/
    index.ts
    vat-settings-page/
      vat-settings-page.component.ts
      vat-settings-page.component.html
  settings.routes.ts
```

**Model (`models/vat-settings.models.ts`):**
```typescript
export interface VatSettings {
  defaultVatRate: number;
}
```

**API service (`data-access/settings-api.service.ts`):**
```typescript
@Injectable({ providedIn: 'root' })
export class SettingsApiService {
  private readonly http = inject(HttpClient);

  getVatSettings(): Observable<VatSettings> {
    return this.http.get<VatSettings>('/api/settings/vat');
  }

  updateVatSettings(defaultVatRate: number): Observable<VatSettings> {
    return this.http.put<VatSettings>('/api/settings/vat', { defaultVatRate });
  }
}
```

**Smart component: `VatSettingsPageComponent`**
- `ChangeDetectionStrategy.OnPush`, standalone
- Inject `SettingsApiService` and `DestroyRef`
- Signals: `isLoading`, `isSaving`, `saveSuccess`, `saveError`
- On init: load VAT settings, populate a `<select>` with the current `defaultVatRate`
- On save: call `updateVatSettings()`, show success/error banner

**Route:** `/instellingen/btw` -- add to `settings.routes.ts`.

**Routes file (`settings.routes.ts` at domain root):**
```typescript
import { Routes } from '@angular/router';

export const settingsRoutes: Routes = [
  {
    path: 'instellingen',
    children: [
      {
        path: 'btw',
        loadComponent: () =>
          import('./feature/vat-settings-page/vat-settings-page.component').then(
            (m) => m.VatSettingsPageComponent,
          ),
      },
      { path: '', redirectTo: 'btw', pathMatch: 'full' },
    ],
  },
];
```

**Register in app routes:** import `settingsRoutes` and add to the lazy-loaded routes in the main app routing.

**Sidebar nav:** add "Instellingen" link to the sidebar in the shell component, pointing to `/instellingen`.

**Template:**
- Page heading: "BTW-instellingen"
- Description text: "Het standaard BTW-tarief wordt automatisch toegepast op diensten zonder afzonderlijk BTW-tarief."
- A single `<select>` field with label "Standaard BTW-tarief" and options: 0%, 9%, 21%
- "Opslaan" button
- Success banner: "Instellingen opgeslagen"
- Error banner: display error message

**Playwright e2e (add to `apps/chairly-e2e/src/settings.spec.ts`):**
- Navigate to `/instellingen/btw`
- Verify "BTW-instellingen" heading is visible
- Change default VAT to 9%, click "Opslaan"
- Verify success banner appears

---

### F3 — Update service API service and store for VatRate

**Update `libs/chairly/src/lib/services/data-access/service-api.service.ts`:**
Ensure the create and update methods pass `vatRate` in the request body.

**Update `libs/chairly/src/lib/services/data-access/service.store.ts`:**
If the store maps or transforms service data, ensure `vatRate` is included.

This task is about ensuring the data-access layer correctly handles the new `vatRate` field end-to-end with the updated backend.

---

## Acceptance Criteria

- [ ] `VatSettings` entity exists in `Chairly.Domain` with `DefaultVatRate` field
- [ ] EF configuration for `VatSettings` with unique index on `TenantId`
- [ ] `DbSet<VatSettings>` added to `ChairlyDbContext`
- [ ] `Service.VatRate` (nullable decimal) added to Service entity and EF config
- [ ] `GET /api/settings/vat` returns default VAT settings; auto-creates if missing
- [ ] `PUT /api/settings/vat` updates default rate; returns 422 for invalid rates
- [ ] `GenerateInvoice` resolves VAT per line item from `Service.VatRate` falling back to `VatSettings.DefaultVatRate`
- [ ] VAT amount uses incl-VAT formula: `unitPrice * rate / (100 + rate)`
- [ ] Service create/update endpoints accept and store `VatRate`
- [ ] Service response includes `VatRate`
- [ ] VAT rate dropdown ("BTW-tarief") in service form shows 0%, 9%, 21% options
- [ ] VAT settings page at `/instellingen/btw` with default rate select
- [ ] Settings domain created in frontend with proper structure
- [ ] "Instellingen" sidebar link added
- [ ] All user-facing text is Dutch
- [ ] All backend quality checks pass (dotnet build, test, format)
- [ ] All frontend quality checks pass (lint, test, build)
- [ ] Playwright e2e tests pass

## Out of Scope

- Free-entry VAT rate input (only 0%, 9%, 21% are supported)
- Retroactive VAT recalculation on existing invoices
- VAT reporting / totals overview
- Multiple VAT rates on a single invoice line item
- Company-information fields on TenantSettings (separate spec)
- Adding `UnitPriceExclVat` as a stored column on `InvoiceLineItem` (can be derived client-side from `UnitPrice - VatAmount`)
- Changes to the invoice detail page template (already shows BTW% and VAT amounts)
