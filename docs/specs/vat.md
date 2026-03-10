# VAT

## Overview

VAT (BTW) must be tracked per service and per invoice line item. A default VAT rate is configured on a settings page; each individual service can override this default. When an invoice is generated, the VAT rate is captured at that moment and frozen on the invoice line item — it is never updated when service or default VAT rates change later. Prices are entered inclusive of VAT (bijv. € 39.99 incl. 21% BTW). Fixes GitHub issue #37.

## Domain Context

- Bounded context: Services + Billing + Settings
- Key entities involved: `Service`, `InvoiceLineItem`, `VatSettings` (new)
- Ubiquitous language:
  - **VatRate** — percentage of VAT applied to a service price; e.g. 21 means 21%
  - **DefaultVatRate** — tenant-wide fallback VAT rate used when a service has none set
  - **PriceInclVat** — the price entered by the user; includes VAT
  - **VatAmount** — the VAT portion: `PriceInclVat × VatRate / (100 + VatRate)`
  - **PriceExclVat** — `PriceInclVat − VatAmount`

### Business Rules

- Prices are inclusive of VAT. The stored `Service.Price` is the incl-VAT price.
- Each `Service` has an optional `VatRate` (decimal?). When null, the default rate from `VatSettings.DefaultVatRate` is applied.
- Common VAT rates in the Netherlands: 0%, 9%, 21%. Users select from these options (not a free-entry field).
- When generating an invoice, for each line item: capture `VatRate` as it is at that moment. Subsequent changes to the service's VAT rate do not affect existing invoices.
- `VatAmount = round(UnitPrice × VatRate / (100 + VatRate), 2)`
- `UnitPriceExclVat = round(UnitPrice − VatAmount, 2)`
- `TotalAmount` on `Invoice` remains the sum of `UnitPrice × Quantity` (incl-VAT amounts) — no change to the total calculation.

---

## Backend Tasks

### B1 — VatSettings entity, EF configuration, and migration

Create a new `VatSettings` entity to store the tenant-wide default VAT rate.

**Domain — `Chairly.Domain/Entities/VatSettings.cs`:**
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

**EF Configuration — `Chairly.Infrastructure/Configurations/VatSettingsConfiguration.cs`:**
- Table: `VatSettings`
- Unique index on `TenantId`
- `DefaultVatRate` precision `(5, 2)` — e.g. 21.00, 9.00, 0.00
- Add `DbSet<VatSettings> VatSettings { get; set; }` to `ChairlyDbContext`

**Migration:** `AddVatSettings`

---

### B2 — Add VatRate to Service entity and migration

Add `VatRate` (nullable decimal) to the `Service` entity.

**Update `Chairly.Domain/Entities/Service.cs`:**
Add:
```csharp
public decimal? VatRate { get; set; }
```
After the `Price` field.

**Update `Chairly.Infrastructure/Configurations/ServiceConfiguration.cs` (or the existing InitialCreate setup):**
- `VatRate` column: precision `(5, 2)`, nullable.

**Migration:** `AddVatRateToService`

**Update `CreateService` handler:** accept `VatRate?` in the command; store it on the entity.

**Update `UpdateService` handler:** accept `VatRate?` in the command; update it on the entity.

**Update `GetService` / `GetServicesList` handlers:** include `VatRate` in the response.

**Update `CreateServiceCommand.cs`:**
Add `public decimal? VatRate { get; init; }` field.
Validation: if not null, must be one of `0`, `9`, `21` (the valid Dutch VAT rates).

**Update `UpdateServiceCommand.cs`:**
Add `public decimal? VatRate { get; init; }` field.
Same validation.

**Update `ServiceResponse.cs`:**
Add `public decimal? VatRate { get; init; }` field.

**Tests:**
- Creating a service with VatRate=21 stores it correctly
- Creating a service with null VatRate stores null
- Creating a service with VatRate=15 returns 422 (invalid rate)
- Updating a service updates VatRate correctly
- GetService returns VatRate in response

---

### B3 — Add VAT fields to InvoiceLineItem and update GenerateInvoice

Add VAT snapshot fields to `InvoiceLineItem` and update the invoice generation logic.

**Update `Chairly.Domain/Entities/InvoiceLineItem.cs`:**
Add:
```csharp
public decimal VatRate { get; set; }
public decimal VatAmount { get; set; }
public decimal UnitPriceExclVat { get; set; }
```

**Update `Chairly.Infrastructure/Configurations/InvoiceConfiguration.cs`:**
- `VatRate` precision `(5, 2)`, not nullable
- `VatAmount` precision `(18, 2)`, not nullable
- `UnitPriceExclVat` precision `(18, 2)`, not nullable

**Migration:** `AddVatToInvoiceLineItems`

**Update `GenerateInvoice` handler:**
1. Load `VatSettings` for tenant; if none exists, create with `DefaultVatRate = 21`.
2. For each `BookingService`, resolve the effective VAT rate:
   - Load the `Service` entity to get `Service.VatRate`.
   - `effectiveVatRate = service.VatRate ?? vatSettings.DefaultVatRate`
3. Calculate:
   - `vatAmount = Math.Round(unitPrice * effectiveVatRate / (100 + effectiveVatRate), 2)`
   - `unitPriceExclVat = unitPrice - vatAmount`
4. Set `lineItem.VatRate = effectiveVatRate`, `lineItem.VatAmount = vatAmount`, `lineItem.UnitPriceExclVat = unitPriceExclVat`.

**Update `InvoiceLineItem` in `InvoiceResponse`:**
Add `VatRate`, `VatAmount`, `UnitPriceExclVat` to the response DTO.

**Tests:**
- GenerateInvoice captures VatRate=21 from service on line item
- GenerateInvoice falls back to DefaultVatRate when service.VatRate is null
- VatAmount calculation is correct: price 39.99, rate 21 → vatAmount = round(39.99 × 21/121, 2) = 6.94
- UnitPriceExclVat = UnitPrice - VatAmount = 33.05

---

### B4 — VAT settings endpoints

**Slice:** `Chairly.Api/Features/Settings/GetVatSettings/` and `Chairly.Api/Features/Settings/UpdateVatSettings/`

**GET /api/settings/vat:**
- Return `VatSettings` for tenant. Auto-create with `DefaultVatRate = 21` if missing.
- Owner and Manager access.

**PUT /api/settings/vat:**
- Request body: `{ "defaultVatRate": 21.0 }`
- Validation: `defaultVatRate` must be one of `0`, `9`, `21`.
- Update and return `200 OK`.
- Owner only.

**Response DTO (`VatSettingsResponse.cs`):**
```json
{ "defaultVatRate": 21.0 }
```

**Tests:**
- GET auto-creates with defaultVatRate=21
- PUT updates to 9 successfully
- PUT returns 422 for invalid rate (e.g. 15)
- PUT returns 403 for non-Owner

---

## Frontend Tasks

### F1 — Update service models and form with VatRate

**Update service models** to include `VatRate`:

In `libs/chairly/src/lib/services/models/service.model.ts` (or equivalent model file):
- Add `vatRate: number | null` to `Service` interface
- Add `vatRate: number | null` to `CreateServiceRequest` interface
- Add `vatRate: number | null` to `UpdateServiceRequest` interface

**Update the service form component** (in `libs/chairly/src/lib/services/`):
- Add a `vatRate` field to the form `FormGroup`
- Render as a `<select>` dropdown with options: `0%`, `9%`, `21%`
  - Option values: `0`, `9`, `21`; default selected: `21`
  - Allow null/"geen" option? No — always require a selection; the field is pre-filled with 21 when creating and with the service's existing rate when editing
- Label: "BTW-tarief"
- Map the form value to `vatRate` in the save event

**Note:** read the existing service form component carefully before editing; it may be `ServiceFormDialogComponent` or similar.

---

### F2 — VAT settings page

**Location:** `libs/chairly/src/lib/settings/feature/vat-settings-page/` (extend the settings domain)

If the settings domain does not yet exist (it may have been created by the company-information feature), create it following the same pattern. If it already exists, add this page to it.

**API service** (`data-access/settings-api.service.ts`): add methods:
```typescript
getVatSettings(): Observable<VatSettings>
updateVatSettings(defaultVatRate: number): Observable<VatSettings>
```

**Model** (`models/vat-settings.model.ts`):
```typescript
export interface VatSettings {
  defaultVatRate: number;
}
```

**Smart component:** `VatSettingsPageComponent`

**Route:** `/instellingen/btw` — add to `settings.routes.ts`.

**Template:**
- Page heading: "BTW-instellingen"
- Description: "Het standaard BTW-tarief wordt automatisch toegepast op diensten zonder afzonderlijk BTW-tarief."
- A single `<select>` field: "Standaard BTW-tarief" with options 0%, 9%, 21%
- "Opslaan" button
- Success and error banners (same pattern as company info page)

**If a settings overview page (`/instellingen`) already exists (from company-information spec):** add a link or tab "BTW" to it pointing to `/instellingen/btw`.

**If no settings page exists yet:** create a simple settings index at `/instellingen` that links to both "Bedrijfsinformatie" and "BTW-instellingen".

**Playwright e2e (add to `apps/chairly-e2e/src/settings.spec.ts`):**
- Navigate to `/instellingen/btw`
- Verify "BTW-instellingen" heading
- Change default VAT to 9%, click "Opslaan"
- Verify success banner

---

### F3 — Update invoice line item display with VAT breakdown

Update the invoice detail page to show the VAT breakdown per line item.

**Location:** Find the `InvoiceDetailPageComponent` in `libs/chairly/src/lib/billing/feature/invoice-detail/`.

**Update model:** add `vatRate: number`, `vatAmount: number`, `unitPriceExclVat: number` to the `InvoiceLineItem` interface in `libs/chairly/src/lib/billing/models/`.

**Update the line items table template:**
- Add columns: "Prijs excl. BTW" | "BTW%" | "BTW bedrag" between "Stukprijs" and "Totaal"
- "Stukprijs" column now shows the incl-VAT price (existing behavior, no change needed)
- New "Prijs excl. BTW" column: shows `unitPriceExclVat` formatted as currency
- New "BTW%" column: shows `vatRate` with `%` suffix (e.g. "21%")
- New "BTW bedrag" column: shows `vatAmount` formatted as currency

**Invoice totals section:** optionally add a summary line: "Waarvan BTW: {totalVatAmount}" where `totalVatAmount = sum of all line item vatAmounts`.

**Playwright e2e (add to existing billing e2e or create new):**
- Generate an invoice from a completed booking
- Navigate to invoice detail
- Verify the VAT columns appear in the line items table
- Verify BTW% column shows the expected rate

---

## Acceptance Criteria

- [ ] `VatSettings` entity exists in `Chairly.Domain` with `DefaultVatRate` field
- [ ] `Service.VatRate` (nullable decimal) added to Service entity and EF config
- [ ] `InvoiceLineItem` has `VatRate`, `VatAmount`, `UnitPriceExclVat` fields
- [ ] `GET /api/settings/vat` returns default VAT settings; auto-creates if missing
- [ ] `PUT /api/settings/vat` updates default rate; returns 422 for invalid rates
- [ ] `POST /api/invoices` (GenerateInvoice) captures VAT per line item at generation time
- [ ] VAT rate is resolved: service VatRate if set, else DefaultVatRate
- [ ] Service create/update endpoints accept and store `VatRate`
- [ ] Service response includes `VatRate`
- [ ] VAT rate dropdown in service form shows 0%, 9%, 21% options
- [ ] VAT settings page at `/instellingen/btw` with default rate select
- [ ] Invoice detail shows VAT breakdown columns
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
