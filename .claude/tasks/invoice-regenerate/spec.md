# Invoice Regenerate

## Overview

Once an invoice is generated from a booking, there is no way to regenerate it if the user made a mistake (e.g. forgot to add a service to the booking before generating the invoice). This feature adds a "Factuur opnieuw genereren" action on the invoice detail page, available for invoices in **Concept** status only. Regenerating replaces the existing invoice's line items and total with the current state of the booking's services. Fixes GitHub issue #50.

## Domain Context

- **Bounded context:** Billing
- **Key entities involved:** `Invoice`, `InvoiceLineItem`, `Booking`, `BookingService`, `Service`, `VatSettings`
- **Ubiquitous language:** Invoice (factuur), Booking (boeking), Concept (draft status), Verzonden (sent), Betaald (paid), Vervallen (voided)
- **Business rule:** Only **Concept** invoices (`SentAtUtc == null && PaidAtUtc == null && VoidedAtUtc == null`) can be regenerated. Sent, Paid, and Void invoices are immutable.

## Backend Tasks

### B1 — Regenerate invoice endpoint

**Slice:** `Chairly.Api/Features/Billing/RegenerateInvoice/`

Files to create:
- `RegenerateInvoiceCommand.cs`
- `RegenerateInvoiceHandler.cs`
- `RegenerateInvoiceEndpoint.cs`

**Command:**

```csharp
internal sealed record RegenerateInvoiceCommand(Guid Id) : IRequest<OneOf<InvoiceResponse, NotFound, Unprocessable>>;
```

The command takes only the invoice ID. The handler resolves the associated booking internally.

**Route:** `POST /api/invoices/{id:guid}/regenerate`

**Endpoint:** Follow the same pattern as `VoidInvoiceEndpoint` — extract `Guid id` from route, create `RegenerateInvoiceCommand(id)`, send via mediator, match result to `200 OK`, `404`, or `422`.

```csharp
group.MapPost("/{id:guid}/regenerate", async (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
{
    var result = await mediator.Send(new RegenerateInvoiceCommand(id), cancellationToken).ConfigureAwait(false);
    return result.Match(
        invoice => Results.Ok(invoice),
        _ => Results.NotFound(),
        unprocessable => Results.UnprocessableEntity(new { message = unprocessable.Message }));
});
```

**Handler logic (RegenerateInvoiceHandler):**

1. Load invoice by `id` scoped to `TenantId`, including `LineItems`. Return `NotFound` if not found.
2. Verify invoice is in Concept status: `SentAtUtc == null && PaidAtUtc == null && VoidedAtUtc == null`. Return `Unprocessable("Alleen concept-facturen kunnen opnieuw worden gegenereerd")` if not.
3. Load the associated booking by `invoice.BookingId` scoped to `TenantId`, including `BookingServices`. Return `NotFound` if booking not found.
4. Verify `booking.CompletedAtUtc != null`. Return `Unprocessable("Boeking is niet afgerond")` if not (edge case: booking was uncompleted after invoice was generated).
5. Resolve `VatSettings` for the tenant (same pattern as `GenerateInvoiceHandler.ResolveVatSettingsAsync`).
6. Load the `Service` entities for the booking's service IDs to get per-service VAT rates (same pattern as `GenerateInvoiceHandler.BuildInvoiceAsync`).
7. Build new line items from `BookingServices` using the same `BuildLineItems` logic as `GenerateInvoiceHandler` — description from `ServiceName`, quantity 1, unit/total price from `Price`, VAT from service or default rate, `IsManual = false`.
8. Clear `invoice.LineItems` and replace with the newly built line items.
9. Recalculate totals using `InvoiceMapper.RecalculateInvoiceTotals(invoice)`.
10. Keep `InvoiceNumber`, `InvoiceDate`, `CreatedAtUtc`, `CreatedBy` unchanged — only line items and totals are updated.
11. Persist via `SaveChangesAsync`.
12. Load `clientFullName` and return `InvoiceMapper.ToResponse(invoice, clientFullName)`.

**Implementation note:** The line-item building logic (steps 5-7) duplicates what is in `GenerateInvoiceHandler`. Consider extracting `BuildLineItems` and `ResolveVatSettingsAsync` into a shared helper (e.g. `InvoiceLineItemBuilder` in the Billing feature folder) to avoid duplication. Alternatively, keep the duplication if the team prefers simplicity in the first iteration.

**Register endpoint:** Add `group.MapRegenerateInvoice();` to `BillingEndpoints.cs` after the existing endpoint registrations.

**Response shape:** Same `InvoiceResponse` record already used by other billing endpoints — no new response type needed.

**Tests to add to `InvoiceHandlerTests.cs`:**

1. **Returns 200 with updated line items when booking services have changed** — Create a completed booking with services A and B. Generate an invoice. Modify the booking to have services A, B, and C. Call regenerate. Assert line items now include all three services and totals are recalculated.
2. **Returns 422 when invoice is Verzonden** — Create an invoice with `SentAtUtc` set. Call regenerate. Assert `Unprocessable` result with message `"Alleen concept-facturen kunnen opnieuw worden gegenereerd"`.
3. **Returns 422 when invoice is Betaald** — Create an invoice with `PaidAtUtc` set. Call regenerate. Assert `Unprocessable` result.
4. **Returns 422 when invoice is Vervallen** — Create an invoice with `VoidedAtUtc` set. Call regenerate. Assert `Unprocessable` result.
5. **Returns 404 when invoice not found** — Call regenerate with a non-existent ID. Assert `NotFound` result.
6. **Returns 422 when booking is not completed** — Create an invoice linked to a booking that has no `CompletedAtUtc`. Call regenerate. Assert `Unprocessable` with message `"Boeking is niet afgerond"`.
7. **Preserves InvoiceNumber, InvoiceDate, CreatedAtUtc, CreatedBy** — Regenerate a valid invoice and verify these fields remain unchanged.

### B2 — Extract shared line-item building logic

**Refactoring task** (optional but recommended): Extract the duplicated line-item building logic from `GenerateInvoiceHandler` into a shared helper class so both `GenerateInvoiceHandler` and `RegenerateInvoiceHandler` can reuse it.

**File:** `Chairly.Api/Features/Billing/InvoiceLineItemBuilder.cs`

```csharp
internal sealed class InvoiceLineItemBuilder(ChairlyDbContext db)
{
    public async Task<List<InvoiceLineItem>> BuildFromBookingAsync(
        IEnumerable<BookingService> bookingServices,
        CancellationToken cancellationToken) { ... }

    private async Task<VatSettings> ResolveVatSettingsAsync(CancellationToken cancellationToken) { ... }
}
```

Move `BuildLineItems` (static) and `ResolveVatSettingsAsync` from `GenerateInvoiceHandler` into this class. Update `GenerateInvoiceHandler` to use the new builder. Verify all existing `GenerateInvoice` tests still pass after the refactoring.

## Frontend Tasks

### F1 — Add `regenerateInvoice` method to `InvoiceApiService`

**File:** `libs/chairly/src/lib/billing/data-access/invoice-api.service.ts`

Add a new method:

```typescript
regenerateInvoice(id: string): Observable<Invoice> {
  return this.http.post<Invoice>(`${this.baseUrl}/invoices/${id}/regenerate`, null);
}
```

No request body is needed — the invoice ID is in the URL path.

### F2 — Add `regenerateInvoice` method to `InvoiceStore`

**File:** `libs/chairly/src/lib/billing/data-access/invoice.store.ts`

Add a new store method following the same pattern as `markAsSent`, `markAsPaid`, `voidInvoice`:

```typescript
regenerateInvoice(id: string): void {
  patchState(store, { isLoading: true, error: null });
  invoiceApi
    .regenerateInvoice(id)
    .pipe(take(1))
    .subscribe({
      next: (updated) =>
        patchState(store, (state) => ({
          selectedInvoice: updated,
          invoices: replaceInvoiceSummary(state.invoices, updated),
          isLoading: false,
        })),
      error: (err: unknown) =>
        patchState(store, {
          error: toErrorMessage(err),
          isLoading: false,
        }),
    });
}
```

Note: This method sets `isLoading: true` before the request and `isLoading: false` on completion, unlike the other status-change methods. This is because regeneration replaces line items and the user should see a loading indicator while the operation is in progress.

### F3 — Add regenerate button to invoice detail page

**Files to modify:**
- `libs/chairly/src/lib/billing/feature/invoice-detail-page/invoice-detail-page.component.ts`
- `libs/chairly/src/lib/billing/feature/invoice-detail-page/invoice-detail-page.component.html`

**Component (`.ts`):**

Add an `onRegenerate()` method:

```typescript
protected onRegenerate(): void {
  const inv = this.invoice();
  if (inv) {
    this.invoiceStore.regenerateInvoice(inv.id);
  }
}
```

No new computed signals are needed — the existing `isDraft` signal already covers the visibility condition (`inv.status === 'Concept'`).

**Template (`.html`):**

Add a "Factuur opnieuw genereren" button in the action buttons section (the `<div class="print-hidden mt-6 flex gap-3">` block). Place it after the "Afdrukken" button and before the "Markeer als verzonden" button. It should only be visible when `isDraft()` is true.

```html
@if (isDraft()) {
  <button
    type="button"
    class="rounded-md border border-gray-300 bg-white px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-primary-500 focus:ring-offset-2 dark:border-slate-600 dark:bg-slate-700 dark:text-gray-300 dark:hover:bg-slate-600"
    [disabled]="isLoading()"
    (click)="onRegenerate()">
    Factuur opnieuw genereren
  </button>
}
```

**Button styling:** Secondary/outline style (same as "Afdrukken" / "Toeslag toevoegen" buttons), distinct from the primary "Markeer als verzonden" button to avoid confusion.

**Visibility rules:**
- Only shown when invoice status is `'Concept'` (using existing `isDraft` computed signal)
- Disabled while `isLoading()` is true (loading state during regeneration)
- Hidden for Verzonden, Betaald, and Vervallen invoices

**UX flow:**
1. User clicks "Factuur opnieuw genereren"
2. Button is disabled, loading state shown
3. On success: invoice detail refreshes automatically (store patches `selectedInvoice`), line items and totals update in-place
4. On error: error state is set in the store (existing error handling)

### F4 — Playwright e2e tests for invoice regenerate

**File:** `apps/chairly-e2e/src/billing.spec.ts`

Add the following test scenarios at the end of the existing billing e2e test file:

**Test 1: "Factuur opnieuw genereren button is visible on Concept invoice"**
- Setup: mock `GET /api/invoices/inv-1` returning a Concept invoice
- Navigate to `/facturen/inv-1`
- Assert button with text "Factuur opnieuw genereren" is visible

**Test 2: "Factuur opnieuw genereren button is hidden on Verzonden invoice"**
- Setup: mock returning a Verzonden invoice (with `sentAtUtc` set)
- Navigate to `/facturen/inv-1`
- Assert button with text "Factuur opnieuw genereren" is not visible

**Test 3: "Factuur opnieuw genereren button is hidden on Betaald invoice"**
- Setup: mock returning a Betaald invoice (with `paidAtUtc` set)
- Navigate to `/facturen/inv-1`
- Assert button "Factuur opnieuw genereren" is not visible

**Test 4: "clicking Factuur opnieuw genereren updates line items"**
- Setup: mock `GET /api/invoices/inv-1` returning Concept invoice with 2 line items. Mock `POST /api/invoices/inv-1/regenerate` returning updated invoice with 3 line items (simulating a service was added to the booking).
- Navigate to `/facturen/inv-1`
- Click "Factuur opnieuw genereren"
- Assert the new third line item description is now visible on the page
- Assert the updated total is visible

**Mock data for regenerated invoice:**
```typescript
const mockRegeneratedInvoice = {
  ...mockInvoiceDetail,
  lineItems: [
    ...mockInvoiceDetail.lineItems,
    {
      id: 'li-3',
      description: 'Haarkleuring',
      quantity: 1,
      unitPrice: 50,
      totalPrice: 50,
      vatPercentage: 21,
      vatAmount: 10.5,
      isManual: false,
      sortOrder: 2,
    },
  ],
  subTotalAmount: 94.21,
  totalVatAmount: 20.79,
  totalAmount: 115,
};
```

## Acceptance Criteria

- [ ] `POST /api/invoices/{id}/regenerate` endpoint exists and replaces line items with current booking services
- [ ] Regenerate returns 422 when invoice is not in Concept status
- [ ] Regenerate returns 422 when associated booking is not completed
- [ ] Regenerate returns 404 when invoice or booking is not found
- [ ] `InvoiceNumber`, `InvoiceDate`, `CreatedAtUtc`, `CreatedBy` remain unchanged after regeneration
- [ ] Line items are rebuilt from current `BookingServices` with correct VAT calculations
- [ ] Totals (`SubTotalAmount`, `TotalVatAmount`, `TotalAmount`) are recalculated
- [ ] "Factuur opnieuw genereren" button appears on invoice detail page for Concept invoices only
- [ ] Button is not shown for Verzonden, Betaald, or Vervallen invoices
- [ ] On success, the detail page refreshes and shows updated line items and totals
- [ ] All backend quality checks pass (`dotnet build`, `dotnet test`, `dotnet format`)
- [ ] All frontend quality checks pass (`nx lint`, `nx format:check`, `nx test`, `nx build`)
- [ ] Playwright e2e tests pass

## Out of Scope

- Regenerating Verzonden, Betaald, or Vervallen invoices (immutable by design)
- Changing the invoice number or invoice date on regeneration
- Voiding and re-creating an invoice as a credit note flow
- Role-based authorization (Owner only) — deferred until Keycloak auth integration is implemented
- Confirmation dialog before regenerating — can be added later if desired
