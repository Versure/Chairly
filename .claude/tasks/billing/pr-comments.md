# PR #34 Review Comments
# Branch: feat/billing
# Title: feat(billing): add invoice generation and management
# Fetched: 2026-03-10T14:35:50Z

---

## Inline Code Comments

_No inline comments._

---

## Review Summaries

_No review summaries._

---

## General Comments

### Comment by Versure

It does not use the loading indicator that is used on other domains.

### Comment by Versure

Conflicts should be resolved

### Comment by Versure

I want to be able to add surcharges and discounts to a generated invoice

### Comment by Versure

Also VAT should be part of an invoice, we need to be able to set VAT aswell, as VAT percentage could be updated by law in the future

### Comment by Versure

## Rework complete

Addressed all review comments. Changes pushed to `feat/billing`.

**Backend fixes:**
- Added VAT support: `VatPercentage` (default 21%) and `VatAmount` on each line item, `SubTotalAmount` and `TotalVatAmount` on invoice. VAT percentage is stored per line item so historical invoices retain their original rate.
- Added surcharges/discounts: `POST /api/invoices/{id}/line-items` to add manual line items (positive = surcharge, negative = discount), `DELETE /api/invoices/{id}/line-items/{lineItemId}` to remove manual items. `IsManual` flag prevents deletion of auto-generated booking items. Only allowed in Draft state.
- New migration `AddInvoiceVatAndManualLineItems` for schema changes.
- 35 handler tests covering all 8 billing slices.

**Frontend fixes:**
- Replaced plain `<p>Laden...</p>` with shared `<chairly-loading-indicator>` component on both invoice pages (matching other domains).
- Added "BTW %" column to line items table and subtotal/BTW/totaal breakdown in invoice detail.
- Added "Toeslag toevoegen" and "Korting toevoegen" buttons (Concept state only) with `LineItemFormDialogComponent` for adding manual line items with configurable VAT percentage.
- Manual line items show a "Verwijderen" button; auto-generated items do not.
- Updated TypeScript models with new VAT and manual line item fields.

**Quality gates:** backend ✓ frontend ✓

### Comment by Versure

When adding a discount no VAT should be entered, the discount is always 0% VAT.

### Comment by Versure

We should be able to edit a sent invoice and resend the invoice afterwards.

### Comment by Versure

On the invoice overview page i want to navigate to the details when clicking on the row.

### Comment by Versure

I want filter options on the invoice list page. We should be able to filter on a customer name and date and also invoice status.

### Comment by Versure

THe total prices do not align under the total column

### Comment by Versure

I want to be able to print an invoice

### Comment by Versure

On the customer detail page i should be able to view the invoices for that customer
