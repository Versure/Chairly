# Revenue Report

> **Status: Implemented** — Merged to main.

## Overview

Revenue report feature for accountants. Salon Owners and Managers can generate weekly or monthly revenue reports showing all paid invoices with daily subtotals, payment methods, and VAT breakdown. Reports can be previewed in-browser and downloaded as PDF. No personal client data is included (privacy-safe). This feature also adds a `PaymentMethod` field to the Invoice entity, requiring an update to the "mark as paid" flow.

---

## Domain Context

- **Bounded context:** Reports (new, backend + frontend), Billing (extended with PaymentMethod)
- **Key entities:** Invoice, InvoiceLineItem, TenantSettings
- **New enum:** PaymentMethod (Cash, Pin, BankTransfer) — required on Invoice
- **Ubiquitous language:**
  - **Invoice** — a billing document generated from a completed booking
  - **Revenue Report** — an accountant-focused summary of paid invoices for a given period
  - **Payment Method** — how an invoice was paid (Contant, Pin, Overboeking)
- **Domain model deviation:** The domain model states "Manage billing/invoices: Owner only". Per explicit decision, Managers are also permitted to mark invoices as paid. The `POST /api/invoices/{id}/pay` endpoint uses `RequireManager` policy (Owner + Manager). All other billing operations remain Owner-only.

---

## Backend Tasks

### B1 — Add PaymentMethod enum and extend Invoice entity

**New enum** in `Chairly.Domain/Enums/PaymentMethod.cs`:

```csharp
namespace Chairly.Domain.Enums;

public enum PaymentMethod
{
    Cash = 0,
    Pin = 1,
    BankTransfer = 2,
}
```

**Extend `Invoice` entity** (`Chairly.Domain/Entities/Invoice.cs`):

Add after `PaidBy`:
```csharp
public PaymentMethod PaymentMethod { get; set; }
```

Required field. Since we are not yet in production, the migration will set all existing invoices to `Pin`. Unpaid invoices will also carry a default value (`Pin`) which gets overwritten when the invoice is marked as paid.

**EF Configuration** — update `InvoiceConfiguration.cs`:

```csharp
builder.Property(i => i.PaymentMethod)
    .IsRequired()
    .HasConversion<string>()
    .HasMaxLength(20);
```

Store as string for readability in the database. Use `HasConversion<string>()`.

**Migration** — create a new idempotent migration adding the `PaymentMethod` column:

```sql
DO $$ BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_name = 'Invoices' AND column_name = 'PaymentMethod'
  ) THEN
    ALTER TABLE "Invoices" ADD COLUMN "PaymentMethod" text NOT NULL DEFAULT 'Pin';
  END IF;
END $$;

-- Set all existing invoices to Pin (safe since we are not yet in production)
UPDATE "Invoices" SET "PaymentMethod" = 'Pin' WHERE "PaymentMethod" IS NULL;
```

**Test cases:**
- Migration applies cleanly on empty database
- Existing invoices have `PaymentMethod = 'Pin'` after migration
- PaymentMethod stores as string (Cash, Pin, BankTransfer)

---

### B2 — Update MarkInvoicePaid to accept PaymentMethod

**Authorization:** The `POST /api/invoices/{id}/pay` endpoint must use the `RequireManager` policy (Owner + Manager). This is a deviation from the domain model which states "Manage billing/invoices: Owner only". The decision was made explicitly to allow Managers to mark invoices as paid.

**Update `MarkInvoicePaidCommand`** to include the payment method:

```csharp
internal sealed record MarkInvoicePaidCommand(Guid Id, PaymentMethod PaymentMethod)
    : IRequest<OneOf<InvoiceResponse, NotFound, Unprocessable>>;
```

Add `[Required]` and `[EnumDataType(typeof(PaymentMethod))]` data annotations on the `PaymentMethod` property (or validate via the enum type directly since it is a required parameter).

**Update `MarkInvoicePaidHandler`:**

When marking as paid, also set:
```csharp
invoice.PaymentMethod = command.PaymentMethod;
```

**Update `MarkInvoicePaidEndpoint`:**

The endpoint currently uses `MapPost("/{id:guid}/pay")` with only the route parameter `id`. It must now accept a request body:

```csharp
internal sealed record MarkInvoicePaidRequest(PaymentMethod PaymentMethod);
```

Update the endpoint to read from the body:
```csharp
group.MapPost("/{id:guid}/pay", async (
    Guid id,
    MarkInvoicePaidRequest request,
    IMediator mediator,
    CancellationToken cancellationToken) =>
{
    var result = await mediator.Send(
        new MarkInvoicePaidCommand(id, request.PaymentMethod),
        cancellationToken).ConfigureAwait(false);
    return result.Match(
        invoice => Results.Ok(invoice),
        _ => Results.NotFound(),
        unprocessable => Results.UnprocessableEntity(new { message = unprocessable.Message }));
});
```

Ensure the endpoint's authorization uses `RequireManager` policy so both Owners and Managers can mark invoices as paid.

**Update `InvoiceResponse`:**

Add `PaymentMethod` field (string, required):
```csharp
string PaymentMethod
```

**Update `InvoiceMapper.ToResponse`** to include `invoice.PaymentMethod.ToString()`.

**Update `InvoiceSummaryResponse`** to include `string PaymentMethod` as well.

**Test cases:**
- MarkInvoicePaid with valid PaymentMethod sets `PaidAtUtc`, `PaidBy`, and `PaymentMethod`
- MarkInvoicePaid with PaymentMethod `Cash` stores correctly
- MarkInvoicePaid with PaymentMethod `Pin` stores correctly
- MarkInvoicePaid with PaymentMethod `BankTransfer` stores correctly
- InvoiceResponse includes `paymentMethod` field after marking paid
- Existing tests for MarkInvoicePaid still pass (update test to send PaymentMethod)
- Owner can mark invoice as paid (RequireManager policy)
- Manager can mark invoice as paid (RequireManager policy)

---

### B3 — GetRevenueReport query and handler

**Slice location:** `Chairly.Api/Features/Reports/GetRevenueReport/`

**Query:**

```csharp
internal sealed record GetRevenueReportQuery(
    [property: Required] string Period,
    DateOnly Date)
    : IRequest<OneOf<RevenueReportResponse, Unprocessable>>;
```

- `Period` has `[Required]` data annotation — validated automatically by the `ValidationBehavior` pipeline
- `Period` must be `"week"` or `"month"` (further validated in handler)
- `Date` is any date within the desired period; the handler calculates boundaries

**Response records** in `Chairly.Api/Features/Reports/`:

```csharp
// RevenueReportResponse.cs
internal sealed record RevenueReportResponse(
    string PeriodType,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    string SalonName,
    IReadOnlyList<RevenueReportRow> Rows,
    IReadOnlyList<RevenueReportDailyTotal> DailyTotals,
    RevenueReportGrandTotal GrandTotal);

// RevenueReportRow.cs
internal sealed record RevenueReportRow(
    DateOnly Date,
    string InvoiceNumber,
    decimal TotalAmount,
    decimal VatAmount,
    string PaymentMethod);

// RevenueReportDailyTotal.cs
internal sealed record RevenueReportDailyTotal(
    DateOnly Date,
    decimal TotalAmount,
    decimal VatAmount,
    int InvoiceCount);

// RevenueReportGrandTotal.cs
internal sealed record RevenueReportGrandTotal(
    decimal TotalAmount,
    decimal VatAmount,
    int InvoiceCount);
```

**Handler logic:**

1. Validate `Period` is `"week"` or `"month"`. If invalid, return `Unprocessable`.
2. Calculate period boundaries:
   - Week: find ISO Monday of the week containing `Date`, end = Monday + 6 days (Sunday)
   - Month: first day of the month containing `Date`, end = last day of that month
3. Query paid invoices (where `PaidAtUtc != null` and `VoidedAtUtc == null`) within the period:
   ```csharp
   var invoices = await db.Invoices
       .Include(i => i.LineItems)
       .Where(i => i.TenantId == tenantContext.TenantId
           && i.PaidAtUtc != null
           && i.VoidedAtUtc == null
           && i.InvoiceDate >= periodStart
           && i.InvoiceDate <= periodEnd)
       .OrderBy(i => i.InvoiceDate)
       .ThenBy(i => i.InvoiceNumber)
       .ToListAsync(cancellationToken)
       .ConfigureAwait(false);
   ```
4. Load salon name from `TenantSettings.CompanyName` (fallback to `"Onbekend"`)
5. Map to `RevenueReportRow` (one per invoice):
   - `VatAmount` = `invoice.TotalVatAmount`
   - `PaymentMethod` = `invoice.PaymentMethod.ToString()`
6. Group rows by date for `DailyTotals`
7. Calculate `GrandTotal` from all rows

**Endpoint** in `Chairly.Api/Features/Reports/GetRevenueReport/GetRevenueReportEndpoint.cs`:

```csharp
group.MapGet("/revenue", async (
    [FromQuery] string period,
    [FromQuery] DateOnly date,
    IMediator mediator,
    CancellationToken cancellationToken) =>
{
    var result = await mediator.Send(
        new GetRevenueReportQuery(period, date), cancellationToken).ConfigureAwait(false);
    return result.Match(
        report => Results.Ok(report),
        unprocessable => Results.UnprocessableEntity(new { message = unprocessable.Message }));
});
```

**Endpoints registration** in `Chairly.Api/Features/Reports/ReportsEndpoints.cs`:

```csharp
internal static class ReportsEndpoints
{
    public static IEndpointRouteBuilder MapReportsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reports")
            .RequireAuthorization("RequireManager");

        group.MapGetRevenueReport();
        group.MapGetRevenueReportPdf();

        return app;
    }
}
```

Register in `Program.cs` alongside existing `MapBillingEndpoints()`.

**Test cases:**
- Returns correct period boundaries for a week query
- Returns correct period boundaries for a month query
- Returns only paid, non-voided invoices within period
- Returns empty rows/totals for period with no paid invoices
- Returns correct daily totals (grouped by date)
- Returns correct grand total
- Returns salon name from TenantSettings
- Returns 422 for invalid period value
- Requires `RequireManager` authorization

---

### B4 — Revenue report PDF generator

**Slice location:** `Chairly.Api/Features/Reports/GetRevenueReportPdf/`

**Interface** in `Chairly.Api/Features/Reports/`:

```csharp
internal interface IRevenueReportPdfGenerator
{
    byte[] Generate(RevenueReportResponse data);
}
```

**Implementation** in `Chairly.Api/Features/Reports/RevenueReportPdfGenerator.cs`:

Uses QuestPDF, following the same pattern as `InvoicePdfGenerator`. A4 page with:
- **Header:** Salon name + "Omzetrapport" title + period (e.g. "Week 13: 23 maart - 29 maart 2026" or "Maart 2026")
- **Content:** Table with columns: Datum | Factuurnummer | Bedrag (incl. BTW) | BTW | Betaalmethode
  - Rows grouped by day with a subtotal row after each day's invoices (shaded background)
  - Grand total row at the bottom (bold)
- **Footer:** Page numbers, generation timestamp

All text in Dutch. Use `CultureInfo("nl-NL")` for formatting.

Register `IRevenueReportPdfGenerator` / `RevenueReportPdfGenerator` in DI (same pattern as invoice PDF).

**Test cases:**
- PDF generator produces non-empty byte array
- PDF contains salon name
- PDF contains invoice rows

---

### B5 — GetRevenueReportPdf endpoint

**Slice location:** `Chairly.Api/Features/Reports/GetRevenueReportPdf/`

**Query:**

```csharp
internal sealed record GetRevenueReportPdfQuery(
    [property: Required] string Period,
    DateOnly Date)
    : IRequest<OneOf<byte[], Unprocessable>>;
```

- `Period` has `[Required]` data annotation — validated automatically by the `ValidationBehavior` pipeline

**Handler:**

1. Reuse the same period calculation and invoice query logic from `GetRevenueReportHandler` (extract shared logic into a `RevenueReportBuilder` helper class in `Chairly.Api/Features/Reports/`)
2. Pass the `RevenueReportResponse` to `IRevenueReportPdfGenerator.Generate()`
3. Return the byte array

**Endpoint:**

```csharp
group.MapGet("/revenue/pdf", async (
    [FromQuery] string period,
    [FromQuery] DateOnly date,
    IMediator mediator,
    CancellationToken cancellationToken) =>
{
    var result = await mediator.Send(
        new GetRevenueReportPdfQuery(period, date), cancellationToken).ConfigureAwait(false);
    return result.Match(
        pdf => Results.File(pdf, "application/pdf", $"omzetrapport-{period}-{date:yyyy-MM-dd}.pdf"),
        unprocessable => Results.UnprocessableEntity(new { message = unprocessable.Message }));
});
```

**Test cases:**
- Returns PDF content type for valid request
- Returns correct filename pattern
- Returns 422 for invalid period

---

## Frontend Tasks

### F1 — Add PaymentMethod to billing models and update mark-as-paid flow

**Shared PaymentMethod type** — create `libs/shared/src/lib/models/payment-method.model.ts`:

The `PaymentMethod` type and `paymentMethodLabels` constant live in `shared/` because they are used across two domains (billing and reports). Sheriff rules prohibit cross-domain imports, so shared is the correct location.

```typescript
export type PaymentMethod = 'Cash' | 'Pin' | 'BankTransfer';

export const paymentMethodLabels: Record<PaymentMethod, string> = {
  Cash: 'Contant',
  Pin: 'Pin',
  BankTransfer: 'Overboeking',
};
```

Export from `libs/shared/src/lib/models/index.ts` (create barrel if needed) and ensure re-export from `libs/shared/src/index.ts`.

**Models** — update `libs/chairly/src/lib/billing/models/invoice.models.ts`:

```typescript
import { PaymentMethod } from '@org/shared-lib';

export interface MarkInvoicePaidRequest {
  paymentMethod: PaymentMethod;
}
```

Add `paymentMethod` to `Invoice` and `InvoiceSummary`:
```typescript
paymentMethod: PaymentMethod;
```

**Update `InvoiceApiService`:**

The `markAsPaid()` method must now send the payment method in the request body:
```typescript
markAsPaid(id: string, paymentMethod: PaymentMethod): Observable<Invoice> {
  return this.http.post<Invoice>(`${this.baseUrl}/${id}/pay`, { paymentMethod });
}
```

**Update `InvoiceStore`:**

Update the `markAsPaid` method to accept `PaymentMethod` parameter and pass it to the API service.

**Update invoice detail page:**

When the user clicks "Markeer als betaald", show a dialog/dropdown to select the payment method before confirming. Three options:
- Contant (Cash)
- Pin (Pin)
- Overboeking (BankTransfer)

The mark-as-paid button must be visible to both Owners and Managers (matching the backend `RequireManager` policy). This is a deviation from the domain model which states "Manage billing/invoices: Owner only".

Use a `<dialog>` element following the existing pattern (full-screen overlay with `showModal()`). The dialog shows:
- Title: "Betaalmethode selecteren"
- Three radio buttons or a dropdown: Contant, Pin, Overboeking
- Buttons: "Bevestigen" (primary), "Annuleren" (secondary)

After selection, call `invoiceStore.markAsPaid(id, paymentMethod)`.

**Display payment method** on the invoice detail page in the metadata section:
- Label: "Betaalmethode"
- Always display the Dutch label from `paymentMethodLabels` (imported from `@org/shared-lib`), since all invoices have a payment method

**Test cases:**
- MarkAsPaid sends payment method in request body
- Payment method dialog opens on "Markeer als betaald" click
- Payment method displays on paid invoice detail

---

### F2 — Reports domain scaffolding (models, service, store)

**Directory:** `libs/chairly/src/lib/reports/`

Create the reports domain structure:
```
reports/
  models/
    index.ts
    report.models.ts
  data-access/
    index.ts
    report-api.service.ts
    report.store.ts
  feature/
    index.ts
    revenue-report-page/
      revenue-report-page.component.ts
      revenue-report-page.component.html
      revenue-report-page.component.scss
  reports.routes.ts
```

**No `ui/` directory needed.** The revenue report page is a single smart component that renders the period selector, report table, and totals inline. The page is read-only and display-focused, so extracting presentational sub-components would add overhead without meaningful reuse. If future report types are added, presentational components can be extracted then.

**Models** (`report.models.ts`):

```typescript
import { PaymentMethod } from '@org/shared-lib';

export type PeriodType = 'week' | 'month';

export interface RevenueReportRow {
  date: string;
  invoiceNumber: string;
  totalAmount: number;
  vatAmount: number;
  paymentMethod: PaymentMethod;
}

export interface RevenueReportDailyTotal {
  date: string;
  totalAmount: number;
  vatAmount: number;
  invoiceCount: number;
}

export interface RevenueReportGrandTotal {
  totalAmount: number;
  vatAmount: number;
  invoiceCount: number;
}

export interface RevenueReport {
  periodType: PeriodType;
  periodStart: string;
  periodEnd: string;
  salonName: string;
  rows: RevenueReportRow[];
  dailyTotals: RevenueReportDailyTotal[];
  grandTotal: RevenueReportGrandTotal;
}
```

Note: `RevenueReportRow.paymentMethod` uses the `PaymentMethod` type (from `@org/shared-lib`), NOT `string`. This ensures type consistency with the `PaymentMethod` type defined in F1. The type lives in `shared/` because it is used across two domains (billing and reports), and Sheriff rules prohibit cross-domain imports.

**API Service** (`report-api.service.ts`):

```typescript
@Injectable()
export class ReportApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/reports';

  getRevenueReport(period: PeriodType, date: string): Observable<RevenueReport> {
    return this.http.get<RevenueReport>(`${this.baseUrl}/revenue`, {
      params: { period, date },
    });
  }

  downloadRevenueReportPdf(period: PeriodType, date: string): Observable<Blob> {
    return this.http.get(`${this.baseUrl}/revenue/pdf`, {
      params: { period, date },
      responseType: 'blob',
    });
  }
}
```

**Store** (`report.store.ts`):

NgRx SignalStore with:
- State: `report: RevenueReport | null`, `loading: boolean`, `error: string | null`, `selectedPeriod: PeriodType`, `selectedDate: string`
- Methods:
  - `loadReport(period, date)` — calls `ReportApiService.getRevenueReport()`, updates state
  - `downloadPdf(period, date)` — calls `ReportApiService.downloadRevenueReportPdf()`, triggers browser download. Must use `inject(DOCUMENT)` (from `@angular/common`) to access the document object for creating the anchor element — never use the global `document` directly. Uses `URL.createObjectURL` + anchor element click pattern.
  - `setPeriod(period)` / `setDate(date)` — updates selection and reloads

**Sheriff config** — update `sheriff.config.ts` to add reports domain:

```typescript
'reports/<layer>': ['domain:reports', 'layer:<layer>'],
```

And add to `chairly-lib` deps:
```typescript
'domain:reports',
```

**Barrel exports:**
- Export `reportsRoutes` from `libs/chairly/src/index.ts`

**Test cases:**
- ReportApiService calls correct URL with query params
- Store loads report and updates state
- Store handles error state

---

### F3 — Revenue report page component

**Component:** `libs/chairly/src/lib/reports/feature/revenue-report-page/`

Smart component that:
1. Reads `periode` and `datum` from query params (via `ActivatedRoute`)
2. Defaults to `periode=week` and `datum=<today>` if not provided
3. Maps Dutch query params to English API params before calling the service: `periode` -> `period`, `datum` -> `date`
4. Calls `reportStore.loadReport(period, date)` on init and when params change

**Query param mapping:** The URL uses Dutch query params (`periode`, `datum`) for user-facing consistency, but the API service (`ReportApiService`) uses English params (`period`, `date`) matching the backend API. The component or store must map `periode` -> `period` and `datum` -> `date` when reading from the route and calling the API.

**Subscription cleanup:** The `ActivatedRoute.queryParams` subscription must use `takeUntilDestroyed(destroyRef)` with an explicitly injected `DestroyRef`. Inject `DestroyRef` via the constructor or `inject(DestroyRef)` and pass it to `takeUntilDestroyed(destroyRef)`. Never use the `Subject` + `ngOnDestroy` teardown pattern.

**No `ui/` sub-components.** This page renders the period selector, report table, and totals inline as a single smart component. See F2 rationale.

**Template layout:**

```
Page header: "Omzetrapport"

[Period selector bar]
  Toggle: "Week" / "Maand" (two buttons, active state highlighted)
  Date navigation: "<" [current period label] ">"
    - Week: "Week 13: 23 mrt - 29 mrt 2026"
    - Month: "Maart 2026"
  [PDF downloaden] button (right-aligned, primary style)

[Report preview — white card, document-style like invoice detail]
  Header: Salon name + period label

  Table:
    Columns: Datum | Factuurnummer | Bedrag (incl. BTW) | BTW | Betaalmethode

    Rows grouped by day:
      - Invoice rows (normal)
      - Day subtotal row (bg-gray-100 dark:bg-slate-700, bold)

    Grand total row (bg-gray-200 dark:bg-slate-600, bold, larger text)

  Empty state (when no invoices):
    "Geen betaalde facturen in deze periode."

  Loading state:
    Spinner with "Rapport laden..."
```

**Period navigation:**
- When user clicks "<" or ">", adjust the date by -/+ 1 week or -/+ 1 month
- Update query params via `Router.navigate([], { queryParams: ... })`
- This triggers a reload via the query param subscription

**PDF download:**
- "PDF downloaden" button calls `reportStore.downloadPdf(periode, datum)`
- The `downloadPdf` method in the store (or a helper) must use `inject(DOCUMENT)` (from `@angular/common`) to access the document object for creating the download anchor element. Never use the global `document` directly.
- Show loading spinner on button while downloading
- Filename: `omzetrapport-{period}-{date}.pdf`

**Payment method display:**
- Import `paymentMethodLabels` from `@org/shared-lib`
- Show Dutch labels: Contant, Pin, Overboeking
- All invoices have a payment method, so no null fallback is needed

**Dark mode:** All custom colors must have `dark:` variants. Document card uses `bg-white dark:bg-slate-800`.

**All UI copy in Dutch:**
- "Omzetrapport", "Week", "Maand"
- "PDF downloaden", "Rapport laden..."
- "Geen betaalde facturen in deze periode."
- "Datum", "Factuurnummer", "Bedrag (incl. BTW)", "BTW", "Betaalmethode"
- "Subtotaal", "Totaal"
- "Contant", "Pin", "Overboeking"

**Test cases (Vitest):**
- Component initializes with default period and date
- Period toggle switches between week and month
- Navigation arrows change date by correct increment
- Loading state shown while fetching

---

### F4 — Reports route registration and navigation

**Routes** (`reports.routes.ts`):

```typescript
export const reportsRoutes: Route[] = [
  {
    path: '',
    component: RevenueReportPageComponent,
    providers: [ReportStore, ReportApiService],
  },
];
```

**App routes** — update `apps/chairly/src/app/app.routes.ts`:

Add after the `facturen` route:
```typescript
{
  path: 'rapporten',
  canActivate: [roleGuard('manager')],
  loadChildren: () => import('@org/chairly-lib').then((m) => m.reportsRoutes),
},
```

**Navigation** — update `libs/shared/src/lib/ui/shell/shell.component.html`:

Add a "Rapporten" nav item right after "Facturen", inside the same `@if (authStore.isManager())` block. Use a chart/document icon (Heroicons `ChartBarIcon` or `DocumentChartBarIcon`):

```html
<li>
  <a
    routerLink="/rapporten"
    routerLinkActive="bg-primary-600"
    (click)="closeSidebar()"
    class="flex items-center px-3 py-2 rounded-md text-sm font-medium text-white hover:bg-primary-600 transition-colors">
    <svg xmlns="http://www.w3.org/2000/svg" class="h-5 w-5 mr-2" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M3 13.125C3 12.504 3.504 12 4.125 12h2.25c.621 0 1.125.504 1.125 1.125v6.75C7.5 20.496 6.996 21 6.375 21h-2.25A1.125 1.125 0 013 19.875v-6.75zM9.75 8.625c0-.621.504-1.125 1.125-1.125h2.25c.621 0 1.125.504 1.125 1.125v11.25c0 .621-.504 1.125-1.125 1.125h-2.25a1.125 1.125 0 01-1.125-1.125V8.625zM16.5 4.125c0-.621.504-1.125 1.125-1.125h2.25C20.496 3 21 3.504 21 4.125v15.75c0 .621-.504 1.125-1.125 1.125h-2.25a1.125 1.125 0 01-1.125-1.125V4.125z" />
    </svg>
    Rapporten
  </a>
</li>
```

**Test cases:**
- "Rapporten" nav item visible for Manager and Owner roles
- "Rapporten" nav item hidden for Staff Member role
- `/rapporten` route loads the revenue report page
- `/rapporten` route requires manager role guard

---

### F5 — Playwright e2e tests for revenue report

**File:** `apps/chairly-e2e/src/reports.spec.ts`

**Mock data:**

```typescript
const mockRevenueReport = {
  periodType: 'week',
  periodStart: '2026-03-23',
  periodEnd: '2026-03-29',
  salonName: 'Salon Test',
  rows: [
    { date: '2026-03-23', invoiceNumber: '2026-0042', totalAmount: 65.00, vatAmount: 11.30, paymentMethod: 'Pin' },
    { date: '2026-03-23', invoiceNumber: '2026-0043', totalAmount: 45.00, vatAmount: 7.82, paymentMethod: 'Cash' },
    { date: '2026-03-24', invoiceNumber: '2026-0044', totalAmount: 120.00, vatAmount: 20.83, paymentMethod: 'BankTransfer' },
  ],
  dailyTotals: [
    { date: '2026-03-23', totalAmount: 110.00, vatAmount: 19.12, invoiceCount: 2 },
    { date: '2026-03-24', totalAmount: 120.00, vatAmount: 20.83, invoiceCount: 1 },
  ],
  grandTotal: { totalAmount: 230.00, vatAmount: 39.95, invoiceCount: 3 },
};
```

**Mock routes:**
- `GET /api/reports/revenue*` returns `mockRevenueReport`
- `GET /api/reports/revenue/pdf*` returns a minimal PDF blob

**Test cases:**

1. **Revenue report page loads with data:**
   - Navigate to `/rapporten?periode=week&datum=2026-03-23`
   - Verify "Omzetrapport" heading is visible
   - Verify salon name is displayed
   - Verify invoice rows are visible (check for invoice numbers)

2. **Daily subtotals are shown:**
   - Verify subtotal rows appear with correct amounts

3. **Grand total is shown:**
   - Verify grand total row displays total amount and invoice count

4. **Period toggle switches between week and month:**
   - Click "Maand" button
   - Verify URL updates to `periode=month`

5. **Payment methods display in Dutch:**
   - Verify "Pin", "Contant", "Overboeking" labels appear

6. **PDF download button is present:**
   - Verify "PDF downloaden" button exists

7. **Empty state when no invoices:**
   - Mock empty response
   - Verify "Geen betaalde facturen in deze periode." message

---

### F6 — Update billing e2e tests for payment method

**File:** `apps/chairly-e2e/src/billing.spec.ts`

Update existing billing e2e tests to account for the new payment method flow:

1. **Mark as paid shows payment method dialog:**
   - Click "Markeer als betaald" on an unpaid invoice
   - Verify payment method selection dialog appears
   - Verify three options: Contant, Pin, Overboeking

2. **Mark as paid with selected payment method:**
   - Select "Pin" and click "Bevestigen"
   - Verify the API call includes `paymentMethod: "Pin"` in the body
   - Verify invoice detail shows "Betaalmethode: Pin"

3. **Mark as paid visible for both Owner and Manager:**
   - Verify "Markeer als betaald" button is visible when logged in as Owner
   - Verify "Markeer als betaald" button is visible when logged in as Manager

4. **Update existing mock data:**
   - Add `paymentMethod` field to mock invoice responses

---

## Acceptance Criteria

- [ ] `PaymentMethod` enum exists with Cash, Pin, BankTransfer values
- [ ] Invoice entity has required `PaymentMethod` field, stored as string
- [ ] `POST /api/invoices/{id}/pay` requires `paymentMethod` in the request body
- [ ] `POST /api/invoices/{id}/pay` uses `RequireManager` policy (Owner + Manager can mark as paid)
- [ ] "Mark as paid" UI shows payment method selection dialog
- [ ] "Mark as paid" button visible for both Owners and Managers
- [ ] Paid invoices display payment method in Dutch (Contant, Pin, Overboeking)
- [ ] `GET /api/reports/revenue?period=week&date=...` returns JSON report for the week
- [ ] `GET /api/reports/revenue?period=month&date=...` returns JSON report for the month
- [ ] `GET /api/reports/revenue/pdf?period=week&date=...` returns downloadable PDF
- [ ] Report shows one row per paid invoice, grouped by day with daily subtotals
- [ ] Report includes grand total with total amount, VAT, and invoice count
- [ ] Report includes payment method per invoice row
- [ ] No personal client data in the report (no names, emails, phones)
- [ ] "Rapporten" nav item visible for Owner and Manager, hidden for Staff Member
- [ ] `/rapporten` route is guarded with `roleGuard('manager')`
- [ ] Period toggle between week and month works correctly
- [ ] Date navigation (prev/next period) updates report
- [ ] PDF download works and file is named correctly
- [ ] Empty state shown when no paid invoices in period
- [ ] All UI copy is in Dutch
- [ ] Dark mode works correctly on the report page
- [ ] `ActivatedRoute.queryParams` uses `takeUntilDestroyed(destroyRef)` with injected `DestroyRef`
- [ ] `DOCUMENT` token used via `inject(DOCUMENT)` for DOM access (no global `document`)
- [ ] `PaymentMethod` type and `paymentMethodLabels` are in `libs/shared/src/lib/models/payment-method.model.ts`
- [ ] `RevenueReportRow.paymentMethod` uses `PaymentMethod` type (not `string`)
- [ ] All backend quality checks pass (`dotnet build`, `dotnet test`, `dotnet format`)
- [ ] All frontend quality checks pass (`lint`, `format:check`, `test`, `build`)
- [ ] Playwright e2e tests pass

---

## Out of Scope

- Filtering by payment method within the report
- Exporting to CSV/Excel (PDF only for now)
- Email sending of reports (download only)
- Tax declaration integration
- Multi-period comparison / charts / graphs
- Custom date range (only full week or full month)
- Revenue per staff member or per service breakdown
- Per-invoice payment method migration (all existing invoices are set to Pin in the migration)
