# Invoice Layout

## Overview

The invoice detail page currently shows invoice data in a plain table/list format without a proper invoice document layout. This feature transforms the invoice detail page into a structured document layout with a company header, client information, invoice metadata, line items table with VAT breakdown, and a footer showing IBAN, VAT number, and payment terms. The backend `GET /api/invoices/{id}` response is extended with a `ClientSnapshot` object and a `StaffMemberName` field so the frontend has all the data it needs without cross-domain imports. Fixes GitHub issue #51.

---

## Domain Context

- **Bounded context:** Billing (backend + frontend), Settings (backend only — company info lookup)
- **Key entities:** Invoice, InvoiceLineItem, Client, Booking, StaffMember, TenantSettings
- **Ubiquitous language:**
  - **Invoice** — a billing document generated from a completed booking
  - **Client** — a person who receives services at the salon (never "customer")
  - **Staff Member** — a person who works at the salon (never "employee")
  - **Booking** — a scheduled visit by a client with a staff member

---

## Backend Tasks

### B1 — Extend GetInvoice response with ClientSnapshot and StaffMemberName

**Slice:** `Chairly.Api/Features/Billing/GetInvoice/`

The current `GetInvoiceHandler` only resolves `clientFullName` by joining to the `Clients` table. For the invoice layout, we also need the client's email, phone, and address, plus the staff member name from the booking.

**New DTO** — add a `ClientSnapshotResponse` record in `Chairly.Api/Features/Billing/`:

```csharp
internal sealed record ClientSnapshotResponse(
    string FullName,
    string? Email,
    string? Phone,
    string? Address);
```

The `Address` is composed from the `Client` entity fields — but note that `Client` does not have address fields in the domain model. The `Client` entity only has `FirstName`, `LastName`, `Email`, `PhoneNumber`, and `Notes`. Since the existing spec says "for client address/email/phone, extend the response with a ClientSnapshot" and the Client entity has no address, the `Address` field should be `null` for now (address fields can be added to Client in a future story). The snapshot still includes the field so the frontend layout is ready.

**Update `InvoiceResponse`:**

Add two new fields after `ClientFullName`:
- `ClientSnapshot ClientSnapshot` — populated from the `Clients` table
- `string StaffMemberName` — populated by joining through `Bookings` to `StaffMembers`

The existing `ClientFullName` field stays for backward compatibility (it is used by the list page and summary).

**Update `GetInvoiceHandler`:**

1. After loading the invoice, also load the client record:
   ```csharp
   var client = await db.Clients
       .FirstOrDefaultAsync(c => c.Id == invoice.ClientId, cancellationToken)
       .ConfigureAwait(false);
   ```

2. Load the staff member name by joining through the booking:
   ```csharp
   var staffMemberName = await db.Bookings
       .Where(b => b.Id == invoice.BookingId)
       .Join(db.StaffMembers, b => b.StaffMemberId, s => s.Id, (b, s) => s.FirstName + " " + s.LastName)
       .FirstOrDefaultAsync(cancellationToken)
       .ConfigureAwait(false) ?? string.Empty;
   ```

3. Create the `ClientSnapshotResponse`:
   ```csharp
   var clientSnapshot = new ClientSnapshotResponse(
       clientFullName,
       client?.Email,
       client?.PhoneNumber,
       null); // Client entity has no address fields yet
   ```

**Update `InvoiceMapper.ToResponse`:**

Add `clientSnapshot` and `staffMemberName` parameters and pass them through to the `InvoiceResponse` constructor.

**Endpoint:** No route changes — same `GET /api/invoices/{id}`, but the response shape is extended.

**Response shape (JSON):**
```json
{
  "id": "...",
  "invoiceNumber": "2026-0001",
  "invoiceDate": "2026-03-10",
  "bookingId": "...",
  "clientId": "...",
  "clientFullName": "Jan de Vries",
  "clientSnapshot": {
    "fullName": "Jan de Vries",
    "email": "jan@example.com",
    "phone": "0612345678",
    "address": null
  },
  "staffMemberName": "Anna de Vries",
  "subTotalAmount": 65.00,
  "totalVatAmount": 11.30,
  "totalAmount": 65.00,
  "status": "Concept",
  "lineItems": [...],
  "createdAtUtc": "...",
  "sentAtUtc": null,
  "paidAtUtc": null,
  "voidedAtUtc": null
}
```

**Tests (add to `InvoiceHandlerTests.cs`):**
- `GetInvoice` returns `clientSnapshot` with correct `fullName`, `email`, `phone`
- `GetInvoice` returns `staffMemberName` from the booking's staff member
- `GetInvoice` returns `clientSnapshot.address` as `null` (no address fields on Client)
- Existing tests still pass (backward-compatible `clientFullName` field)

---

### B2 — Extend InvoiceMapper and other invoice-returning handlers

The `InvoiceMapper.ToResponse` method is used by multiple handlers (GetInvoice, MarkInvoiceSent, MarkInvoicePaid, VoidInvoice, AddInvoiceLineItem, RemoveInvoiceLineItem). All of them must pass the new `ClientSnapshot` and `StaffMemberName` to the mapper.

**Update `InvoiceMapper.ToResponse` signature:**
```csharp
public static InvoiceResponse ToResponse(
    Invoice invoice,
    string clientFullName,
    ClientSnapshotResponse clientSnapshot,
    string staffMemberName)
```

**Update each handler that calls `InvoiceMapper.ToResponse`:**
- `GetInvoiceHandler` — already updated in B1
- `MarkInvoiceSentHandler` — load client + staff member name before mapping
- `MarkInvoicePaidHandler` — load client + staff member name before mapping
- `VoidInvoiceHandler` — load client + staff member name before mapping
- `AddInvoiceLineItemHandler` — load client + staff member name before mapping
- `RemoveInvoiceLineItemHandler` — load client + staff member name before mapping

To avoid duplicating the lookup logic, extract a private helper method or a static method on `InvoiceMapper`:

```csharp
public static async Task<(string ClientFullName, ClientSnapshotResponse ClientSnapshot, string StaffMemberName)>
    LoadInvoiceContextAsync(ChairlyDbContext db, Invoice invoice, CancellationToken cancellationToken)
```

This method:
1. Loads the client from `db.Clients`
2. Loads the staff member name via `db.Bookings` join to `db.StaffMembers`
3. Returns all three values

**Tests:**
- Verify `MarkInvoiceSent` response includes `clientSnapshot` and `staffMemberName`
- Verify `AddLineItem` response includes `clientSnapshot` and `staffMemberName`
- Existing handler tests continue to pass

---

## Frontend Tasks

### F1 — Update Invoice model with ClientSnapshot and StaffMemberName

**File:** `libs/chairly/src/lib/billing/models/invoice.models.ts`

Add the `ClientSnapshot` interface and extend the `Invoice` interface:

```typescript
export interface ClientSnapshot {
  fullName: string;
  email: string | null;
  phone: string | null;
  address: string | null;
}

export interface Invoice {
  // ... existing fields ...
  clientSnapshot: ClientSnapshot;
  staffMemberName: string;
}
```

**Update the barrel export** in `models/index.ts` to export `ClientSnapshot`.

No API service or store changes needed — the existing `getInvoice()` call already returns `Invoice`, which will now include the new fields.

---

### F2 — Load company information on invoice detail page

The invoice detail page needs company info (name, address, email, phone, IBAN, VAT number, payment term) for the document header and footer. Since the billing domain cannot import from the settings domain (Sheriff module boundary rules), the component must use its own mechanism to load company info.

**Approach:** Add a `getCompanyInfo()` method to the `InvoiceApiService` (or create a dedicated service within the billing domain) that calls `GET /api/settings/company`. This is the same endpoint the settings domain uses, but the billing domain makes its own service call without importing from the settings domain.

**File:** `libs/chairly/src/lib/billing/data-access/invoice-api.service.ts`

Add:
```typescript
getCompanyInfo(): Observable<CompanyInfo> {
  return this.http.get<CompanyInfo>(`${this.baseUrl}/settings/company`);
}
```

**File:** `libs/chairly/src/lib/billing/models/invoice.models.ts`

Add the `CompanyInfo` interface (duplicated from settings domain to respect Sheriff boundaries):
```typescript
export interface CompanyInfo {
  companyName: string | null;
  companyEmail: string | null;
  street: string | null;
  houseNumber: string | null;
  postalCode: string | null;
  city: string | null;
  companyPhone: string | null;
  ibanNumber: string | null;
  vatNumber: string | null;
  paymentPeriodDays: number | null;
}
```

**Update `InvoiceStore`:**

Add `companyInfo` to the state:
```typescript
export interface InvoiceState {
  // ... existing fields ...
  companyInfo: CompanyInfo | null;
}
```

Add a `loadCompanyInfo()` method that calls `invoiceApi.getCompanyInfo()` and stores the result.

**Update `InvoiceDetailPageComponent`:**

- In `ngOnInit()`, after `loadInvoice()`, also call `invoiceStore.loadCompanyInfo()`
- Add a computed signal: `company = computed(() => this.invoiceStore.companyInfo())`

**Update barrel exports** in `models/index.ts` and `data-access/index.ts` as needed.

---

### F3 — Redesign invoice detail template with document layout

**File:** `libs/chairly/src/lib/billing/feature/invoice-detail-page/invoice-detail-page.component.html`

Replace the current template with a structured invoice document layout:

```
Back link: "<- Terug naar facturen"
Status badge (outside the document block)

[Document block -- white card, print-friendly padding, bg-white dark:bg-slate-800 rounded-lg shadow p-8]

  [Header -- two-column grid (grid-cols-2)]
    [Left] Bedrijfsinformatie:
      <company name, bold>
      <street + houseNumber>
      <postalCode + city>
      <companyEmail>
      <companyPhone>

    [Right, text-right] Klantinformatie:
      <client fullName, bold>
      <client address> (if present)
      <client email>
      <client phone>

  [Divider -- border-t, my-6]

  [Invoice metadata -- two-column grid]
    [Left]
      Factuurnummer: <invoiceNumber>
      Factuurdatum: <invoiceDate | date:'dd-MM-yyyy'>
      Medewerker: <staffMemberName>

    [Right -- empty]

  [Divider -- border-t, my-6]

  [Body -- line items table]
    Omschrijving | Aantal | Stukprijs | BTW % | Totaal
    (rows from inv.lineItems)
    [Subtotaal row]
    [BTW row]
    [Totaal row -- bold, bg-gray-50 dark:bg-slate-700]

  [Footer -- centered, muted text, border-t, pt-6, mt-6]
    IBAN: <iban>
    BTW-nummer: <vatNumber>
    Betaaltermijn: <paymentPeriodDays> dagen

[Add surcharge/discount buttons -- print-hidden, outside document block]
[Status history -- print-hidden, outside document block]
[Action buttons -- print-hidden, outside document block]
[Line item form dialog]
```

**Dark mode:** All custom/brand colors must have explicit `dark:` variants. The document block uses `bg-white dark:bg-slate-800`. Table header uses `bg-gray-50 dark:bg-slate-700`.

**Print styles:** Keep existing print styles in `.scss` file. The `print-hidden` class hides non-document elements when printing. Consider adding `@media print` overrides to ensure the document block fills the page.

**Component updates:**
- Add `company` signal to the component (from F2)
- Use `inv.clientSnapshot` for client info in the header
- Use `inv.staffMemberName` for the medewerker field
- Use `company()` for IBAN, VAT number, payment terms in footer
- Format company address as `street houseNumber`, `postalCode city`

All user-facing copy must be in Dutch:
- "Bedrijf" section labels
- "Klant" section labels
- "Factuurnummer", "Factuurdatum", "Medewerker"
- "Omschrijving", "Aantal", "Stukprijs", "BTW %", "Totaal"
- "Subtotaal", "BTW", "Totaal"
- "IBAN:", "BTW-nummer:", "Betaaltermijn: X dagen"

---

### F4 — Update print styles for document layout

**File:** `libs/chairly/src/lib/billing/feature/invoice-detail-page/invoice-detail-page.component.scss`

Update the existing print stylesheet to handle the new document layout:

- Ensure the document block `bg-white` is white on print
- Hide all `print-hidden` elements (back link, status badge area, surcharge buttons, status history, action buttons)
- Remove shadows and rounded corners on print
- Ensure the header grid, footer, and table print cleanly
- Set page margins appropriate for A4

---

### F5 — Playwright e2e tests for invoice layout

**File:** `apps/chairly-e2e/src/billing.spec.ts`

Add e2e tests that verify the new invoice layout:

**Mock data updates:**
- Update `mockInvoiceDetail` to include `clientSnapshot` and `staffMemberName` fields
- Add a `mockCompanyInfo` object with company name, address, IBAN, VAT number, payment period

**Mock routes:**
- Add route mock for `GET /api/settings/company` returning `mockCompanyInfo`

**Test cases:**

1. **Invoice detail page shows company information in header:**
   - Navigate to `/facturen/inv-1`
   - Verify company name is visible
   - Verify company email is visible

2. **Invoice detail page shows client information in header:**
   - Navigate to `/facturen/inv-1`
   - Verify client full name is visible
   - Verify client email is visible

3. **Invoice detail page shows invoice metadata:**
   - Navigate to `/facturen/inv-1`
   - Verify "Factuurnummer" label and invoice number are visible
   - Verify "Factuurdatum" label and formatted date are visible
   - Verify "Medewerker" label and staff member name are visible

4. **Invoice detail page shows footer with IBAN and BTW-nummer:**
   - Navigate to `/facturen/inv-1`
   - Verify IBAN value is visible
   - Verify BTW-nummer value is visible
   - Verify "dagen" text for payment terms is visible

---

## Acceptance Criteria

- [ ] `GET /api/invoices/{id}` response includes `clientSnapshot` with `fullName`, `email`, `phone`, `address`
- [ ] `GET /api/invoices/{id}` response includes `staffMemberName`
- [ ] All handlers returning `InvoiceResponse` include the new fields
- [ ] Invoice detail page shows a structured document layout with header, body, and footer sections
- [ ] Header left column shows company name, address, email, and phone number
- [ ] Header right column shows client name, email, and phone number (right-aligned)
- [ ] Invoice details section shows factuurnummer, factuurdatum, and medewerker
- [ ] Footer (centered) shows IBAN, BTW-nummer, and betaaltermijn
- [ ] Line items table with VAT % column and totals are displayed in the body
- [ ] Layout is readable in both light and dark mode
- [ ] Print layout produces a clean, document-style output
- [ ] Existing billing e2e tests pass (backward compatibility)
- [ ] New e2e tests verify the document layout elements
- [ ] All backend quality checks pass (`dotnet build`, `dotnet test`, `dotnet format`)
- [ ] All frontend quality checks pass (`lint`, `format:check`, `test`, `build`)

---

## Out of Scope

- PDF export or dedicated print stylesheet beyond basic @media print rules
- Sending invoices by email
- Storing a company/client snapshot on the `Invoice` entity (current values from joined tables are sufficient for now)
- Adding address fields to the `Client` entity (future story)
- Editable invoice layout or templates
- Changes to the invoice list page
