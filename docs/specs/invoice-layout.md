# Invoice Layout

## Overview

The invoice detail page currently shows invoice data in a plain table/list format without a proper invoice document layout. Company information should appear in the header alongside client information, and the footer should display IBAN, VAT number, and payment terms. This makes the invoice visually suitable for printing and sharing. Fixes GitHub issue #51.

---

## Domain Context

- **Bounded context:** Billing (frontend only — no new backend endpoints needed)
- **Key files involved:**
  - `libs/chairly/src/lib/billing/feature/invoice-detail/invoice-detail-page.component.html`
  - `libs/chairly/src/lib/billing/feature/invoice-detail/invoice-detail-page.component.ts`
  - `libs/chairly/src/lib/settings/data-access/company.service.ts` — to load company info
  - `libs/chairly/src/lib/billing/models/invoice.model.ts` — may need company snapshot on invoice response (see below)

---

## Layout Specification

The invoice document is rendered as a visual document block within the detail page.

### Header

Two-column layout:

| Company information (left-aligned) | Client information (right-aligned) |
|:---|---:|
| Bedrijfsnaam | Naam |
| Adres | Adres |
| E-mailadres | E-mailadres |
| Telefoonnummer | Telefoonnummer |

Below the two-column block, a second row in a two-column layout:

| Invoice details (left-aligned) | (empty right column) |
|:---|---|
| Factuurnummer | |
| Factuurdatum | |
| Medewerker | |

### Body

- Invoice line items table: Omschrijving | Aantal | Stukprijs | Totaal
- Totals row at the bottom

### Footer (centered)

- IBAN
- BTW-nummer
- Betaaltermijn

---

## Frontend Tasks

### F1 — Load company information on invoice detail page

The invoice detail page must load company info (name, address, email, phone number, IBAN, VAT number, payment term) alongside the invoice. Use the existing `CompanyService` (or `SettingsService`) from the settings domain — import via the shared barrel only, no direct cross-domain imports.

**In `InvoiceDetailPageComponent`:**
- Inject `CompanyService` and call `getCompanyInfo()` on init
- Store result as a signal: `company = signal<CompanyInfo | null>(null)`
- The invoice already contains `clientFullName`; for client address/email/phone, extend the `GET /api/invoices/{id}` response with a `ClientSnapshot` object (see backend task B1)

### F2 — Extend invoice detail response with client snapshot

The current invoice detail response only includes `clientFullName`. For the invoice layout we also need the client's address, email, and phone number as they were at invoice creation time (or as current values — see B1).

**Model update** (`libs/chairly/src/lib/billing/models/invoice.model.ts`):

Add `clientSnapshot` to the `Invoice` interface:

```typescript
export interface ClientSnapshot {
  fullName: string;
  email: string;
  phone: string;
  address: string;
}

export interface Invoice {
  // ... existing fields ...
  clientSnapshot: ClientSnapshot;
}
```

### F3 — Redesign invoice detail template

Replace the current invoice detail template layout with a structured invoice document.

**File:** `libs/chairly/src/lib/billing/feature/invoice-detail/invoice-detail-page.component.html`

Structure:
```
Back link: "← Terug naar facturen"
Status badge (outside the document block)

[Document block — white card, print-friendly padding]

  [Header — two-column grid]
    [Left] Company info:
      <company name, bold>
      <address>
      <email>
      <phone>

    [Right, text-right] Client info:
      <client full name, bold>
      <client address>
      <client email>
      <client phone>

  [Divider]

  [Invoice details — two-column grid]
    [Left]
      Factuurnummer: <invoiceNumber>
      Factuurdatum: <invoiceDate | date:'dd-MM-yyyy'>
      Medewerker: <staffMemberName>

    [Right — empty]

  [Divider]

  [Body — line items table]
    Omschrijving | Aantal | Stukprijs | Totaal
    (rows)
    [Totals row — right-aligned]

  [Footer — centered, muted text]
    IBAN: <iban>
    BTW-nummer: <vatNumber>
    Betaaltermijn: <paymentTerm> dagen

[Action buttons below the document block (Owner only)]
```

Use Tailwind classes for layout. The document block uses `bg-white dark:bg-slate-800 rounded-lg shadow p-8`.

---

## Backend Tasks

### B1 — Extend invoice response with client snapshot

**Slice:** `Chairly.Api/Features/Billing/GetInvoice/`

Extend the `GetInvoiceResponse` DTO to include a `ClientSnapshot` object populated by joining to the `Clients` table at query time:

```csharp
public record ClientSnapshot(
    string FullName,
    string Email,
    string Phone,
    string Address
);
```

Add `ClientSnapshot ClientSnapshot` to the invoice response. The staff member name should also be included — join to `StaffMembers` via the booking's `StaffMemberId`.

Similarly extend `GET /api/invoices` list response if needed for display, but the snapshot is primarily needed on the detail endpoint.

**Tests:**
- `GET /api/invoices/{id}` response includes `clientSnapshot` with correct values

---

## Acceptance Criteria

- [ ] Invoice detail page shows a structured document layout with header, body, and footer sections
- [ ] Header left column shows company name, address, email, and phone number
- [ ] Header right column shows client name, address, email, and phone number (right-aligned)
- [ ] Invoice details section shows factuurnummer, factuurdatum, and medewerker
- [ ] Footer (centered) shows IBAN, BTW-nummer, and betaaltermijn
- [ ] Line items table and totals are unchanged in the body
- [ ] `GET /api/invoices/{id}` response includes `clientSnapshot` object
- [ ] Layout is readable in both light and dark mode
- [ ] All backend quality checks pass (dotnet build, test, format)
- [ ] All frontend quality checks pass (lint, test, build)

---

## Out of Scope

- PDF export or print stylesheet
- Sending invoices by email
- Storing a company/client snapshot on the `Invoice` entity (current values from joined tables are sufficient for now)
- Editable invoice layout or templates
