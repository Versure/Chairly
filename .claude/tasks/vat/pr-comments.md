# PR #47 Review Comments
# Branch: feat/vat
# Title: feat(vat): add VAT tracking per service and invoice line item
# Fetched: 2026-03-11T10:52:49Z

---

## Inline Code Comments

_No inline comments._

---

## Review Summaries

_No review summaries._

---

## General Comments

### Comment by Versure

The invoices prices are not calculated correctly. The services prices are prices including VAT, so the invoice should not add VAT to the totals. When a service "Haircut" has 21% VAT and a price of 100 euro's is entered I expect the invoice to display:

Haircut 79 euro excl. VAT
VAT 21 euro

Total: 100 euro

But when adding the Haircut service I want to enter: 100 euro and set VAT to 21%.

### Comment by Versure

## Rework complete

Addressed all review comments. Changes pushed to `feat/vat`.

**Backend fixes:**
- Changed VAT formula from incl-VAT extraction (`price * rate / (100 + rate)`) to simple percentage (`price * rate / 100`) per product owner's requirements
- Updated `SubTotalAmount` to show excl-VAT total: `SubTotalAmount = TotalAmount - TotalVatAmount`
- Updated `RecalculateInvoiceTotals` in `InvoiceMapper` to match the same formula
- Updated all 13 test assertion sites in `InvoiceHandlerTests` to reflect the new calculations

**Example verification:** For a 100 EUR service with 21% VAT:
- Excl. VAT: 79 EUR
- VAT: 21 EUR
- Total: 100 EUR ✓

**Frontend fixes:**
No frontend changes needed — the frontend displays values returned by the API.

**Quality gates:** backend ✓ (193 tests passed, build clean, format clean)

Implemented by the rework-team agent workflow.

### Comment by Versure

Please fix conflicts, make sure you merge the VAT settings with the now existing settings page with company information. Do not remove company information!
