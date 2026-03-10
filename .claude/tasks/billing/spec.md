# Billing (Facturatie)

## Overview

Owners can generate an **Invoice** (factuur) from any completed booking. The invoice captures all booked services as line items, assigns a sequential invoice number scoped to the tenant, and tracks its lifecycle through four states — Draft, Sent, Paid, and Void — using timestamp pairs (no status column, per ADR-009). This feature belongs to the **Billing** bounded context and is the primary mechanism for tracking revenue after a booking is completed.

---

## Domain Context

- **Bounded context:** Billing
- **Key entities involved:** `Invoice` (Aggregate Root), `InvoiceLineItem` (Value Object), `Booking`, `Client`
- **Ubiquitous language:**
  - **Invoice** — a billing document generated from a completed booking
  - **InvoiceLineItem** — a single line on an invoice, derived from a `BookingService` snapshot
  - **Invoice Number** — a tenant-scoped sequential identifier (e.g. `2024-0001`), never reused

### Entities

**`Invoice`** (Aggregate Root)

| Field | Type | Notes |
|---|---|---|
| `Id` | Guid | PK |
| `TenantId` | Guid | FK, multi-tenant scope |
| `BookingId` | Guid | FK → Booking; unique per tenant |
| `ClientId` | Guid | Denormalized from booking for efficient list queries |
| `InvoiceNumber` | string | Sequential per tenant (e.g. `2024-0001`); generated on create |
| `InvoiceDate` | DateOnly | Date the invoice was generated (UTC date) |
| `TotalAmount` | decimal | Sum of all line item totals |
| `LineItems` | List\<InvoiceLineItem\> | Owned collection |
| `CreatedAtUtc` | DateTimeOffset | Required — Draft state |
| `CreatedBy` | Guid | Required |
| `SentAtUtc` | DateTimeOffset? | Set when marked as sent |
| `SentBy` | Guid? | |
| `PaidAtUtc` | DateTimeOffset? | Set when marked as paid |
| `PaidBy` | Guid? | |
| `VoidedAtUtc` | DateTimeOffset? | Set when voided |
| `VoidedBy` | Guid? | |

**`InvoiceLineItem`** (Value Object / owned entity)

| Field | Type | Notes |
|---|---|---|
| `Id` | Guid | PK on owned table |
| `Description` | string | Service name (copied from `BookingService.ServiceName`) |
| `Quantity` | int | Always `1` in this iteration |
| `UnitPrice` | decimal | Copied from `BookingService.Price` |
| `TotalPrice` | decimal | `Quantity × UnitPrice` |
| `SortOrder` | int | Matches `BookingService.SortOrder` |

**Derived status (no status column — ADR-009):**

| Status | Condition |
|---|---|
| **Concept** (Draft) | `CreatedAtUtc` set, all other timestamps null |
| **Verzonden** (Sent) | `SentAtUtc` set |
| **Betaald** (Paid) | `PaidAtUtc` set |
| **Vervallen** (Void) | `VoidedAtUtc` set |

### Business rules

- Invoice can only be generated when `booking.CompletedAtUtc` is set.
- One invoice per booking — unique constraint on `(TenantId, BookingId)`.
- Invoice number is sequential per tenant. Use a database sequence or MAX+1 within a transaction to guarantee uniqueness. Format: `{year}-{sequence:D4}` (e.g. `2024-0001`). Year resets the sequence (each calendar year starts at 0001).
- Line items are copied from `BookingServices` at generation time (immutable after creation).
- `TotalAmount` = sum of all `LineItem.TotalPrice`.
- **Mark as Sent**: only allowed when `VoidedAtUtc` is null and `PaidAtUtc` is null. Idempotent: if already sent, return current state.
- **Mark as Paid**: only allowed when `VoidedAtUtc` is null.
- **Void**: only allowed when `PaidAtUtc` is null.
- **Owner role only** for all write operations (generate, send, pay, void).
- **Owner / Manager**: read all invoices.
- **StaffMember**: can only view invoices for bookings they performed.

---

## Backend Tasks

### B1 — Invoice entity, EF configuration, and migration

Create `Invoice` and `InvoiceLineItem` in **Chairly.Domain**, EF configuration in **Chairly.Infrastructure**, and generate migration.

**Domain — `Chairly.Domain/Entities/Invoice.cs`:**

```csharp
public class Invoice
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid BookingId { get; set; }
    public Guid ClientId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateOnly InvoiceDate { get; set; }
    public decimal TotalAmount { get; set; }
    public List<InvoiceLineItem> LineItems { get; set; } = [];
    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset? SentAtUtc { get; set; }
    public Guid? SentBy { get; set; }
    public DateTimeOffset? PaidAtUtc { get; set; }
    public Guid? PaidBy { get; set; }
    public DateTimeOffset? VoidedAtUtc { get; set; }
    public Guid? VoidedBy { get; set; }
}
```

**Domain — `Chairly.Domain/Entities/InvoiceLineItem.cs`:**

```csharp
public class InvoiceLineItem
{
    public Guid Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public int SortOrder { get; set; }
}
```

**EF Configuration — `Chairly.Infrastructure/Configurations/InvoiceConfiguration.cs`:**

- Table: `Invoices`
- Unique index on `(TenantId, BookingId)` — one invoice per booking
- Unique index on `(TenantId, InvoiceNumber)` — number uniqueness per tenant
- Index on `(TenantId, ClientId)` — for client invoice lookups
- Index on `(TenantId, CreatedAtUtc DESC)` — for list ordering
- `InvoiceNumber` max length 20, required
- `TotalAmount` precision `(18, 2)`
- `LineItems` configured as `OwnsMany` → table `InvoiceLineItems` with FK `InvoiceId`
- `InvoiceLineItem.UnitPrice` and `TotalPrice` precision `(18, 2)`

**Migration:** Add and apply migration `AddInvoices`.

---

### B2 — Generate invoice endpoint

**Slice:** `Chairly.Api/Features/Billing/GenerateInvoice/`

**Route:** `POST /api/invoices`

**Request body:**

```json
{ "bookingId": "guid" }
```

**Handler logic:**

1. Load booking by `bookingId` scoped to `TenantId`. Return `404` if not found.
2. Verify `booking.CompletedAtUtc != null`. Return `422` with `"Boeking is niet afgerond"` if not.
3. Verify no invoice exists for this booking yet. Return `409` with `"Er bestaat al een factuur voor deze boeking"` if one does.
4. Authorisation: Owner role only. Return `403` if caller is Manager or StaffMember.
5. Generate `InvoiceNumber`:
   - Get current year and `MAX(InvoiceNumber)` for tenant in same year within a transaction.
   - Increment sequence; pad to 4 digits. Format: `{year}-{seq:D4}`.
6. Build `Invoice` from booking: copy `ClientId`, map each `BookingService` to an `InvoiceLineItem`, calculate `TotalAmount`.
7. Set `InvoiceDate` = today (UTC), `CreatedAtUtc` = UtcNow, `CreatedBy` = currentUserId.
8. Persist and return `201 Created` with full invoice response.

**Response body:** full invoice (same shape as Get — see B3).

**Tests:**
- Returns 201 with correct line items and total on happy path
- Invoice number increments correctly within a tenant year
- Returns 404 when booking not found
- Returns 422 when booking not completed
- Returns 409 when invoice already exists for booking
- Returns 403 when Manager or StaffMember attempts generation

---

### B3 — Get invoice list endpoint

**Slice:** `Chairly.Api/Features/Billing/GetInvoicesList/`

**Route:** `GET /api/invoices`

**Query parameters:** none required in this iteration (no filtering/pagination yet).

**Handler logic:**

1. Owner/Manager: return all invoices for tenant.
2. StaffMember: join to Booking, filter to `booking.StaffMemberId == currentUserId`.
3. Join to Client for `ClientFullName` (`FirstName + " " + LastName`).
4. Order by `CreatedAtUtc` descending.

**Response body:**

```json
[
  {
    "id": "guid",
    "invoiceNumber": "2024-0001",
    "invoiceDate": "2024-03-10",
    "bookingId": "guid",
    "clientId": "guid",
    "clientFullName": "string",
    "totalAmount": 65.00,
    "status": "Concept|Verzonden|Betaald|Vervallen",
    "createdAtUtc": "datetime",
    "sentAtUtc": "datetime?",
    "paidAtUtc": "datetime?",
    "voidedAtUtc": "datetime?"
  }
]
```

Note: `status` is a derived string computed server-side from the timestamps.

**Tests:**
- Returns empty list when no invoices
- Returns correct status strings for each state
- StaffMember only sees invoices for their own bookings
- Ordered newest first

---

### B4 — Get invoice by ID endpoint

**Slice:** `Chairly.Api/Features/Billing/GetInvoice/`

**Route:** `GET /api/invoices/{id}`

**Handler logic:**

1. Load invoice by `id` scoped to `TenantId`. Return `404` if not found.
2. StaffMember: verify `booking.StaffMemberId == currentUserId` (join needed). Return `403` if not.
3. Return full invoice with line items.

**Response body:**

```json
{
  "id": "guid",
  "invoiceNumber": "2024-0001",
  "invoiceDate": "2024-03-10",
  "bookingId": "guid",
  "clientId": "guid",
  "clientFullName": "string",
  "totalAmount": 65.00,
  "status": "Concept|Verzonden|Betaald|Vervallen",
  "lineItems": [
    {
      "id": "guid",
      "description": "string",
      "quantity": 1,
      "unitPrice": 45.00,
      "totalPrice": 45.00,
      "sortOrder": 0
    }
  ],
  "createdAtUtc": "datetime",
  "sentAtUtc": "datetime?",
  "paidAtUtc": "datetime?",
  "voidedAtUtc": "datetime?"
}
```

**Tests:**
- Returns 200 with line items on happy path
- Returns 404 when invoice not found
- Returns 403 when StaffMember requests another staff member's invoice

---

### B5 — Invoice status transition endpoints

Three separate slices, each following the same pattern.

**Mark as Sent — `Chairly.Api/Features/Billing/MarkInvoiceSent/`**
- Route: `POST /api/invoices/{id}/send`
- Guard: `VoidedAtUtc == null && PaidAtUtc == null`. Return `422` with `"Factuur kan niet als verzonden worden gemarkeerd"` if guard fails.
- Idempotent: if `SentAtUtc` already set, return current invoice without error.
- Set `SentAtUtc = UtcNow`, `SentBy = currentUserId`.
- Owner only.

**Mark as Paid — `Chairly.Api/Features/Billing/MarkInvoicePaid/`**
- Route: `POST /api/invoices/{id}/pay`
- Guard: `VoidedAtUtc == null`. Return `422` with `"Vervallen factuur kan niet als betaald worden gemarkeerd"` if guard fails.
- Idempotent: if `PaidAtUtc` already set, return current invoice.
- Set `PaidAtUtc = UtcNow`, `PaidBy = currentUserId`.
- Owner only.

**Void Invoice — `Chairly.Api/Features/Billing/VoidInvoice/`**
- Route: `POST /api/invoices/{id}/void`
- Guard: `PaidAtUtc == null`. Return `422` with `"Betaalde factuur kan niet vervallen worden verklaard"` if guard fails.
- Set `VoidedAtUtc = UtcNow`, `VoidedBy = currentUserId`.
- Owner only.

All three return `200 OK` with the updated invoice (same shape as B4).

**Tests per transition:**
- Happy path returns 200 with updated timestamps
- Guard violation returns 422 with correct message
- Returns 403 for non-Owner callers
- Returns 404 when invoice not found

---

## Frontend Tasks

### F1 — Invoice models and API service

**Location:** `libs/chairly/src/lib/billing/`

**Models** (`models/invoice.model.ts`):

```typescript
export type InvoiceStatus = 'Concept' | 'Verzonden' | 'Betaald' | 'Vervallen';

export interface InvoiceLineItem {
  id: string;
  description: string;
  quantity: number;
  unitPrice: number;
  totalPrice: number;
  sortOrder: number;
}

export interface Invoice {
  id: string;
  invoiceNumber: string;
  invoiceDate: string;
  bookingId: string;
  clientId: string;
  clientFullName: string;
  totalAmount: number;
  status: InvoiceStatus;
  lineItems: InvoiceLineItem[];
  createdAtUtc: string;
  sentAtUtc?: string;
  paidAtUtc?: string;
  voidedAtUtc?: string;
}

export interface InvoiceSummary {
  id: string;
  invoiceNumber: string;
  invoiceDate: string;
  bookingId: string;
  clientId: string;
  clientFullName: string;
  totalAmount: number;
  status: InvoiceStatus;
  createdAtUtc: string;
  sentAtUtc?: string;
  paidAtUtc?: string;
  voidedAtUtc?: string;
}
```

**API service** (`data-access/invoices.service.ts`):

```typescript
// Methods:
getInvoices(): Observable<InvoiceSummary[]>
getInvoice(id: string): Observable<Invoice>
generateInvoice(bookingId: string): Observable<Invoice>
markAsSent(id: string): Observable<Invoice>
markAsPaid(id: string): Observable<Invoice>
voidInvoice(id: string): Observable<Invoice>
```

---

### F2 — Invoice list page

**Location:** `libs/chairly/src/lib/billing/feature/invoice-list/`

**Route:** `/facturen` (lazy-loaded, Owner/Manager only — add route guard)

**Smart component:** `InvoiceListPageComponent`

Loads invoice list via `InvoicesService.getInvoices()` on init. Displays as a table.

**Template (`invoice-list-page.component.html`):**
- Page heading: "Facturen"
- Table columns: Factuurnummer | Klant | Datum | Totaal | Status | Acties
- Status badge with colour per state:
  - Concept → gray
  - Verzonden → blue
  - Betaald → green
  - Vervallen → red
- "Bekijken" action link per row → navigates to `/facturen/{id}`
- Empty state: "Nog geen facturen beschikbaar"
- Loading state while fetching

**Route registration** in `billing.routes.ts` at the billing domain root:

```typescript
{ path: 'facturen', component: InvoiceListPageComponent },
{ path: 'facturen/:id', component: InvoiceDetailPageComponent },
```

Add "Facturen" nav item to the sidebar (Owner/Manager only, between "Diensten" and any existing entries).

**Playwright e2e:**
- Navigate to /facturen
- Verify empty state when no invoices exist
- After generating an invoice (via booking detail), verify it appears in the list with status "Concept"

---

### F3 — Invoice detail page

**Location:** `libs/chairly/src/lib/billing/feature/invoice-detail/`

**Route:** `/facturen/:id`

**Smart component:** `InvoiceDetailPageComponent`

Loads invoice by ID via `InvoicesService.getInvoice(id)`.

**Template (`invoice-detail-page.component.html`):**
- Back link: "← Terug naar facturen"
- Heading: "Factuur {invoiceNumber}"
- Header section: client name, invoice date, status badge
- Line items table: Omschrijving | Aantal | Stukprijs | Totaal
- Totaal row at the bottom of the table
- Status history section: show each timestamp with label and formatted Dutch date:
  - "Aangemaakt op {date}"
  - "Verzonden op {date}" (only if sentAtUtc set)
  - "Betaald op {date}" (only if paidAtUtc set)
  - "Vervallen verklaard op {date}" (only if voidedAtUtc set)
- Action buttons (Owner only, conditionally rendered per status):
  - Concept → "Markeer als verzonden" (primary) + "Vervallen verklaren" (danger/outline)
  - Verzonden → "Markeer als betaald" (primary) + "Vervallen verklaren" (danger/outline)
  - Betaald → no actions
  - Vervallen → no actions
- Each action button calls the corresponding service method and refreshes the invoice on success

**Playwright e2e:**
- Navigate to invoice detail
- Verify line items match booking services
- Click "Markeer als verzonden" → verify status badge updates to "Verzonden"
- Click "Markeer als betaald" → verify status badge updates to "Betaald"
- Verify "Vervallen verklaren" button is not shown on a paid invoice

---

### F4 — Generate invoice button on booking detail

**Location:** extend existing booking detail view in `libs/chairly/src/lib/bookings/`

On the booking detail view, when `booking.completedAtUtc` is set and no invoice exists yet, show a "Factuur genereren" button (Owner only).

**Behaviour:**
- Button calls `InvoicesService.generateInvoice(bookingId)` on click
- On success: show a success message and a "Factuur bekijken" link that navigates to `/facturen/{invoice.id}`
- On 409 (invoice already exists): show info message "Er bestaat al een factuur voor deze boeking" with a link to the existing invoice
- Loading state on button during request

Note: the booking detail view must import `InvoicesService` from the billing domain via the shared barrel — no direct cross-domain imports.

**Playwright e2e:**
- Navigate to a completed booking detail page
- Click "Factuur genereren"
- Verify success message and "Factuur bekijken" link appear
- Click "Factuur bekijken" → verify navigation to invoice detail with correct line items

---

## Acceptance Criteria

- [ ] `Invoice` entity and `InvoiceLineItem` owned entity exist in Chairly.Domain
- [ ] EF configuration with unique indexes on `(TenantId, BookingId)` and `(TenantId, InvoiceNumber)`
- [ ] `POST /api/invoices` generates an invoice from a completed booking with correct line items and sequential number
- [ ] `POST /api/invoices` returns 422 when booking is not completed
- [ ] `POST /api/invoices` returns 409 when invoice already exists for the booking
- [ ] `GET /api/invoices` returns list with derived status string, newest first
- [ ] `GET /api/invoices/{id}` returns full invoice with line items
- [ ] `POST /api/invoices/{id}/send` transitions to Verzonden; returns 422 if already paid or voided
- [ ] `POST /api/invoices/{id}/pay` transitions to Betaald; returns 422 if voided
- [ ] `POST /api/invoices/{id}/void` transitions to Vervallen; returns 422 if already paid
- [ ] All write endpoints return 403 for non-Owner callers
- [ ] StaffMember can only view their own invoices
- [ ] Invoice list page at /facturen with status badges
- [ ] Invoice detail page shows line items and status history
- [ ] Action buttons appear/disappear correctly per invoice status
- [ ] "Factuur genereren" button on completed booking detail (Owner only)
- [ ] All backend quality checks pass (dotnet build, test, format)
- [ ] All frontend quality checks pass (lint, test, build)
- [ ] Playwright e2e tests pass

---

## Out of Scope

- PDF export of invoices
- Sending invoices by email directly from the app
- Credit notes / partial refunds
- VAT / tax line items
- Invoice editing after creation (invoices are immutable once generated)
- Pagination and filtering on the invoice list
- Payment method tracking (cash, card, etc.)
