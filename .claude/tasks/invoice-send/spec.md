# Invoice Send

> **Status: Implemented** — Merged to main.

## Overview

Replaces the current "Markeer als verzonden" behavior with a real send flow that emails the invoice to the client. The feature remains in the **Billing** context, reuses the existing notifications/email infrastructure, and keeps invoice status derived from timestamps (`SentAtUtc`, `PaidAtUtc`, `VoidedAtUtc`) without introducing status columns.

## Domain Context

- Bounded context: Billing
- Key entities involved: Invoice, Notification, Client
- Ubiquitous language: Invoice, Notification, Client (see docs/domain-model.md)

## User Stories

- As an **Owner**, I want to send an invoice directly to a client from the invoice detail page, so that billing communication happens from one place.
- As an **Owner**, I want invoice sending to update invoice state to Verzonden only after a successful send trigger, so that status reflects real delivery intent.
- As an **Owner/Manager**, I want sent invoice notifications visible in the notification log, so that outbound communication is auditable.

## Backend Tasks

### B1 — Replace MarkInvoiceSent with SendInvoice command flow

- **Slice rename:** `Features/Billing/MarkInvoiceSent` -> `Features/Billing/SendInvoice`.
- **Endpoint:** keep route `POST /api/invoices/{id}/send` for compatibility.
- **Response:** `200 OK` with existing `InvoiceResponse` shape.
- **Validation rules:**
  - `404` when invoice not found for tenant.
  - `422` when invoice is paid or voided.
  - `422` when invoice client has no email address.
- **Handler behavior:**
  - load invoice + related data in tenant scope,
  - trigger send workflow (B2),
  - set `SentAtUtc`/`SentBy` after successful enqueue/dispatch trigger,
  - persist and return mapped response.

### B2 — Queue and dispatch invoice email notification

- Extend notification dispatch rendering to support `InvoiceSent` templates.
- Template content includes at least:
  - invoice number,
  - invoice date,
  - total amount,
  - client name,
  - salon/company name.
- Email subject/body are Dutch.
- Reuse existing `IEmailSender` and `NotificationDispatcher` pipeline.

### B3 — Extend notification type mappings for invoice emails

- Ensure `GET /api/notifications` returns `type = "InvoiceSent"` for invoice-send events.
- Keep existing notification list response schema unchanged.

## Frontend Tasks

### F1 — Replace invoice detail action with Factuur versturen UX

- In invoice detail page, replace button label:
  - from: `Markeer als verzonden`
  - to: `Factuur versturen`.
- Keep button visibility rules aligned with sendability (draft/sendable invoice only).
- Keep action in the existing action-button group.
- Preserve existing visual style unless a dedicated primary send style already exists.
- On success:
  - update local invoice state to `Verzonden`,
  - show Dutch success feedback (existing notification/toast pattern).
- On failure:
  - show API error message in Dutch.

### F2 — Update frontend notification labels and tests for invoice send

- Extend frontend notification type unions/mappings with `InvoiceSent`.
- Add Dutch label mapping, e.g. `Factuur verzonden`.
- Update billing and notifications e2e mocks/assertions where type unions are strict.

## Acceptance Criteria

- [ ] The invoice action is changed from "Markeer als verzonden" to "Factuur versturen" (Dutch UI copy).
- [ ] `POST /api/invoices/{id}/send` performs an actual send workflow instead of only toggling timestamp fields.
- [ ] Sending an invoice creates an outbound notification record and uses the existing email dispatch pipeline.
- [ ] Invoice is only sendable when not voided and not paid; invalid state returns `422` with a Dutch message.
- [ ] Missing client email blocks send with `422` and a clear Dutch validation message.
- [ ] Successful send updates `SentAtUtc` and `SentBy`.
- [ ] Notification log can display the new invoice-send notification type.
- [ ] Existing billing and notification behavior remains intact.
- [ ] All backend quality checks pass (dotnet build, test, format).
- [ ] All frontend quality checks pass (lint, test, build).
- [ ] Playwright e2e tests pass.

## Test Requirements

### Backend

- Unit tests for `SendInvoiceHandler`:
  - happy path sets `SentAtUtc` and queues notification,
  - not found returns `NotFound`,
  - paid/voided returns `Unprocessable`,
  - missing client email returns `Unprocessable`.
- Unit tests for notification dispatch template rendering of `InvoiceSent`.
- Regression test: existing booking notification types still dispatch correctly.

### Frontend

- Unit tests:
  - invoice detail action renders `Factuur versturen` for sendable invoices,
  - API method call uses `POST /api/invoices/{id}/send`,
  - notification label mapper handles `InvoiceSent`.
- Playwright e2e:
  - clicking `Factuur versturen` updates invoice status badge to `Verzonden`,
  - notification log displays the invoice notification type label.

## Out of Scope

- PDF attachment generation or custom invoice email theming.
- Bulk sending multiple invoices in one action.
- Automatic retries specific to invoice sends beyond existing notification retry behavior.
- Changes to payment/void workflows.
