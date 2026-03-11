# Invoice Regenerate

## Overview

Once an invoice is generated from a booking, there is no way to regenerate it if the user made a mistake (e.g. forgot to add a service to the booking before generating the invoice). This feature adds a "Factuur opnieuw genereren" action on the invoice detail page, available for invoices in **Concept** status only. Regenerating replaces the existing invoice's line items and total with the current state of the booking's services. Fixes GitHub issue #50.

---

## Domain Context

- **Bounded context:** Billing
- **Key entities involved:** `Invoice`, `InvoiceLineItem`, `Booking`, `BookingService`
- **Business rule:** Only **Concept** invoices (all status timestamps null except `CreatedAtUtc`) can be regenerated. Sent, Paid, and Void invoices are immutable.

---

## Backend Tasks

### B1 — Regenerate invoice endpoint

**Slice:** `Chairly.Api/Features/Billing/RegenerateInvoice/`

**Route:** `POST /api/invoices/{id}/regenerate`

**Handler logic:**

1. Load invoice by `id` scoped to `TenantId`. Return `404` if not found.
2. Verify invoice is in Concept status (`SentAtUtc == null && PaidAtUtc == null && VoidedAtUtc == null`). Return `422` with `"Alleen concept-facturen kunnen opnieuw worden gegenereerd"` if not.
3. Authorisation: Owner role only. Return `403` otherwise.
4. Load the associated booking with its current `BookingServices`. Return `404` if booking not found.
5. Verify `booking.CompletedAtUtc != null`. Return `422` with `"Boeking is niet afgerond"` if not (edge case: booking was uncompleted after invoice was generated).
6. Replace all existing `InvoiceLineItems` on the invoice with new ones built from the current `BookingServices`.
7. Recalculate `TotalAmount` from the new line items.
8. Keep `InvoiceNumber`, `InvoiceDate`, `CreatedAtUtc`, and `CreatedBy` unchanged — only line items and total are updated.
9. Persist and return `200 OK` with the updated invoice (same shape as `GET /api/invoices/{id}`).

**Tests:**
- Returns 200 with updated line items when booking services have changed
- Returns 422 when invoice is Verzonden
- Returns 422 when invoice is Betaald
- Returns 422 when invoice is Vervallen
- Returns 403 when caller is not Owner
- Returns 404 when invoice not found

---

## Frontend Tasks

### F1 — Add regenerate button to invoice detail page

**Location:** `libs/chairly/src/lib/billing/feature/invoice-detail/invoice-detail-page.component.html`

Add a "Factuur opnieuw genereren" button in the action buttons section of the invoice detail page.

**Visibility:** Only shown when:
- Invoice status is `'Concept'`
- Current user is Owner

**Behaviour:**
- On click: call `InvoicesService.regenerateInvoice(invoice.id)`
- Show a loading state on the button during the request
- On success: reload the invoice detail (refresh signal/observable) and show a success notification: "Factuur is opnieuw gegenereerd"
- On error: show error message from the API response

**Button styling:** secondary/outline style (distinct from primary "Markeer als verzonden" button to avoid confusion)

### F2 — Add `regenerateInvoice` method to `InvoicesService`

**File:** `libs/chairly/src/lib/billing/data-access/invoices.service.ts`

```typescript
regenerateInvoice(id: string): Observable<Invoice>
```

Calls `POST /api/invoices/{id}/regenerate`.

---

## Acceptance Criteria

- [ ] `POST /api/invoices/{id}/regenerate` endpoint exists and replaces line items with current booking services
- [ ] Regenerate returns 422 when invoice is not in Concept status
- [ ] Regenerate returns 403 for non-Owner callers
- [ ] "Factuur opnieuw genereren" button appears on invoice detail page for Concept invoices (Owner only)
- [ ] Button is not shown for Verzonden, Betaald, or Vervallen invoices
- [ ] On success, the detail page refreshes and shows updated line items
- [ ] All backend quality checks pass (dotnet build, test, format)
- [ ] All frontend quality checks pass (lint, test, build)

---

## Out of Scope

- Regenerating Verzonden, Betaald, or Vervallen invoices (immutable by design)
- Changing the invoice number or invoice date on regeneration
- Voiding and re-creating an invoice as a credit note flow
