# Manage Email Templates

> **Status: Implemented** — Merged to main.

## Overview

Allow tenants to customize the email templates used for notifications (booking confirmations, reminders, cancellations, booking received, invoice sent). Tenants can edit structured fields (subject, main message, closing message) with placeholder support, preview the rendered result using sample data from a backend endpoint, and reset individual templates to hardcoded defaults. Owner and Manager roles can manage templates. The system falls back to hardcoded defaults when no custom template row exists for a given template type.

## Domain Context

- Bounded context: Notifications (backend), Settings (frontend page placement)
- Key entities involved: `EmailTemplate` (new), `Notification` (existing), `NotificationDispatcher` (existing infrastructure)
- Ubiquitous language:
  - **EmailTemplate** — a per-tenant, per-template-type record storing customized Subject, MainMessage, and ClosingMessage
  - **TemplateType** — enum reusing `NotificationType`: `BookingConfirmation`, `BookingReminder`, `BookingCancellation`, `BookingReceived`, `InvoiceSent`
  - **Placeholder** — a token like `{clientName}` or `{salonName}` that the system replaces with actual values at send time
  - **Default template** — the hardcoded values in `EmailTemplates.cs` used when no `EmailTemplate` row exists for a tenant + type combination

### Access Control — Exception to Domain Model

The domain model states "Manage tenant settings" is Owner-only. This spec grants access to both Owner and Manager (per user decision 8). Justification: email template management is a communication/marketing task that Managers commonly handle in salon operations. It is distinct from core tenant settings like billing, VAT, or company information. This exception applies only to email templates, not to other tenant settings.

### Domain Model Alignment — NotificationType Enum

The `NotificationType` enum in `Chairly.Domain/Enums/NotificationType.cs` already contains all 5 values (`BookingConfirmation`, `BookingReminder`, `BookingCancellation`, `BookingReceived`, `InvoiceSent`). However, `docs/domain-model.md` only documents three values. Task B1 includes a note to update `docs/domain-model.md` to reflect the full enum.

### Business Rules

- Each tenant can have at most one `EmailTemplate` row per `TemplateType` (unique constraint on `TenantId` + `TemplateType`).
- When no custom template exists, the system uses the existing hardcoded defaults from `EmailTemplates.cs`.
- The UI shows the hardcoded defaults as pre-filled values when creating/editing a template that has no DB row yet.
- "Standaardwaarden herstellen" (reset to default) deletes the DB row, causing the system to fall back to hardcoded defaults.
- The fixed HTML layout (header, greeting, date/service details box, footer) is NOT customizable — only Subject, MainMessage, and ClosingMessage are editable.
- Available placeholders vary per template type:
  - **BookingConfirmation / BookingReminder / BookingReceived**: `{clientName}`, `{salonName}`, `{date}`, `{services}`
  - **BookingCancellation**: `{clientName}`, `{salonName}`, `{date}`
  - **InvoiceSent**: `{clientName}`, `{salonName}`, `{invoiceNumber}`, `{invoiceDate}`, `{totalAmount}`
- Access: Owner and Manager roles.

---

## Backend Tasks

### B1 — EmailTemplate entity, EF configuration, and migration

Create a new `EmailTemplate` entity to store per-tenant customized email template fields.

**Domain — `Chairly.Domain/Entities/EmailTemplate.cs`:**
```csharp
public class EmailTemplate
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public NotificationType TemplateType { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string MainMessage { get; set; } = string.Empty;
    public string ClosingMessage { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid? UpdatedBy { get; set; }
}
```

**EF Configuration — `Chairly.Infrastructure/Persistence/Configurations/EmailTemplateConfiguration.cs`:**
- Table: `EmailTemplates`
- Unique index on `(TenantId, TemplateType)` — at most one row per tenant per type
- `TemplateType` stored as `int` (EF default for enums)
- `Subject` max length 500
- `MainMessage` max length 2000
- `ClosingMessage` max length 1000
- `CreatedBy` required, `UpdatedBy` nullable
- Add `DbSet<EmailTemplate> EmailTemplates { get; set; }` to `ChairlyDbContext`

**Migration:** `AddEmailTemplates` — must be idempotent per CLAUDE.md rules:
- `CREATE TABLE IF NOT EXISTS "EmailTemplates"` via raw SQL
- `CREATE UNIQUE INDEX IF NOT EXISTS` for the `(TenantId, TemplateType)` index

**Domain model documentation:** Update `docs/domain-model.md` section "6. Notifications" to list all 5 `NotificationType` values: `BookingConfirmation`, `BookingReminder`, `BookingCancellation`, `BookingReceived`, `InvoiceSent`. Also add `EmailTemplate` to the entity list under Notifications with its fields and business rules.

**Tests:**
- EmailTemplate entity can be persisted and retrieved
- Unique constraint on (TenantId, TemplateType) prevents duplicates

---

### B2 — Default template values helper

Create a static helper that returns the hardcoded default Subject, MainMessage, ClosingMessage, and AvailablePlaceholders for each `NotificationType`. This helper will be used by the GET endpoint (to return defaults when no DB row exists), the Update handler (to populate AvailablePlaceholders in the response), and the `NotificationDispatcher` (to render emails).

**File:** `Chairly.Api/Features/Notifications/Infrastructure/DefaultEmailTemplateValues.cs`

```csharp
internal static class DefaultEmailTemplateValues
{
    internal sealed record TemplateDefaults(
        string Subject,
        string MainMessage,
        string ClosingMessage,
        string[] AvailablePlaceholders);

    internal static TemplateDefaults GetDefaults(NotificationType type, string salonName)
    {
        return type switch
        {
            NotificationType.BookingConfirmation => new(
                $"Bevestiging van uw afspraak bij {salonName}",
                "Uw afspraak is bevestigd.",
                "Wij kijken ernaar uit u te verwelkomen!",
                ["clientName", "salonName", "date", "services"]),
            NotificationType.BookingReminder => new(
                $"Herinnering: uw afspraak morgen bij {salonName}",
                "Dit is een herinnering dat u morgen een afspraak heeft.",
                "Wij zien u graag!",
                ["clientName", "salonName", "date", "services"]),
            NotificationType.BookingCancellation => new(
                "Uw afspraak is geannuleerd",
                "Uw afspraak is helaas geannuleerd.",
                "Neem gerust contact met ons op als u een nieuwe afspraak wilt maken.",
                ["clientName", "salonName", "date"]),
            NotificationType.BookingReceived => new(
                $"Nieuwe boeking bij {salonName}",
                "Wij hebben uw boeking ontvangen. Uw boeking wacht op bevestiging.",
                "Wij nemen zo snel mogelijk contact met u op.",
                ["clientName", "salonName", "date", "services"]),
            NotificationType.InvoiceSent => new(
                $"Factuur {{invoiceNumber}} van {salonName}",
                "Bedankt voor uw bezoek! Bijgaand vindt u uw factuur.",
                "Wij zien u graag terug!",
                ["clientName", "salonName", "invoiceNumber", "invoiceDate", "totalAmount"]),
            _ => new(string.Empty, string.Empty, string.Empty, []),
        };
    }
}
```

**Tests:**
- GetDefaults returns correct values for each NotificationType
- Subject contains salonName for BookingConfirmation, BookingReminder, BookingReceived, InvoiceSent
- BookingCancellation subject does not contain salonName
- AvailablePlaceholders are correct per template type
- BookingCancellation does not include "services" placeholder
- InvoiceSent includes "invoiceNumber", "invoiceDate", "totalAmount" placeholders

---

### B3 — Get email templates list endpoint

**Slice:** `Chairly.Api/Features/Notifications/GetEmailTemplatesList/`

**GET /api/notifications/email-templates:**
- Query: `GetEmailTemplatesListQuery` implementing `IRequest<List<EmailTemplateResponse>>`
- Handler: load all `EmailTemplate` rows for the tenant. For each of the 5 `NotificationType` values, return a response object. If a custom template exists, use its values; otherwise, use defaults from `DefaultEmailTemplateValues` (passing `salonName` from `TenantSettings.CompanyName`).
- **Cross-context access:** The handler needs `TenantSettings.CompanyName` (Settings bounded context). Since `ChairlyDbContext` already contains `DbSet<TenantSettings>`, the handler may query it directly — EF DbContext is shared infrastructure, not a slice-to-slice reference. This is the same pattern used by `NotificationDispatcher` which already reads `TenantSettings` for salon name. No domain event or shared contract is needed because this is a read-only query against a shared persistence layer, not a cross-slice command/handler invocation.
- Always returns exactly 5 items (one per template type), regardless of how many DB rows exist.
- The handler derives `AvailablePlaceholders` from `DefaultEmailTemplateValues.GetDefaults()` for every template — both customized and default.
- Access: Owner and Manager (`RequireManagerOrOwner` policy).

**Response DTO — `EmailTemplateResponse.cs`:**
```csharp
internal sealed record EmailTemplateResponse(
    string TemplateType,
    string Subject,
    string MainMessage,
    string ClosingMessage,
    bool IsCustomized,
    string[] AvailablePlaceholders);
```

- `TemplateType`: the enum name as string (e.g. `"BookingConfirmation"`)
- `IsCustomized`: `true` if a DB row exists, `false` if using defaults
- `AvailablePlaceholders`: array of placeholder names for this template type (e.g. `["clientName", "salonName", "date", "services"]`)

**Endpoint registration:** Add to `NotificationEndpoints.cs` within the notifications group.

**Tests:**
- Returns 5 templates when no custom templates exist (all IsCustomized=false)
- Returns custom values when DB row exists, with IsCustomized=true
- AvailablePlaceholders are correct per template type
- Returns 200 for Owner/Manager
- Returns 403 for Staff Member

---

### B4 — Update email template endpoint

**Slice:** `Chairly.Api/Features/Notifications/UpdateEmailTemplate/`

**PUT /api/notifications/email-templates/{templateType}:**
- Command: `UpdateEmailTemplateCommand` with:
  - `TemplateType` (string, from route, validated against known enum values)
  - `Subject` (string, required, max 500)
  - `MainMessage` (string, required, max 2000)
  - `ClosingMessage` (string, required, max 1000)
- Handler:
  1. Parse and validate `TemplateType` against `NotificationType` enum. If invalid, return `400 Bad Request` (malformed route parameter).
  2. Look up existing `EmailTemplate` for tenant + type.
  3. If exists: update fields, set `UpdatedAtUtc` and `UpdatedBy`.
  4. If not exists: create new row with `CreatedAtUtc` and `CreatedBy`.
  5. Derive `AvailablePlaceholders` from `DefaultEmailTemplateValues.GetDefaults()` for the given type (same lookup used by B3).
  6. Save and return `EmailTemplateResponse` with `IsCustomized=true` and the derived `AvailablePlaceholders`.
- Return `200 OK` with updated `EmailTemplateResponse`.
- Access: Owner and Manager.

**Validation (Data Annotations):**
- `Subject`: `[Required]`, `[MaxLength(500)]`
- `MainMessage`: `[Required]`, `[MaxLength(2000)]`
- `ClosingMessage`: `[Required]`, `[MaxLength(1000)]`

**Error responses:**
- `400 Bad Request`: invalid `TemplateType` (not a valid enum value)
- `422 Unprocessable Entity`: validation failures on Subject, MainMessage, ClosingMessage (handled by `ValidationBehavior` pipeline)

**Tests:**
- Creating a new custom template returns IsCustomized=true
- Updating an existing template updates the values
- Response includes correct AvailablePlaceholders for the template type
- Invalid TemplateType returns 400
- Empty Subject returns 422
- Subject exceeding 500 chars returns 422

---

### B5 — Reset email template endpoint

**Slice:** `Chairly.Api/Features/Notifications/ResetEmailTemplate/`

**DELETE /api/notifications/email-templates/{templateType}:**
- Command: `ResetEmailTemplateCommand` with `TemplateType` (string, from route)
- Handler:
  1. Parse and validate `TemplateType` against `NotificationType` enum. If invalid, return `400 Bad Request`.
  2. Find `EmailTemplate` for tenant + type.
  3. If exists: delete the row. If not exists: return 204 (idempotent).
  4. Return `204 No Content`.
- Access: Owner and Manager.

**Error responses:**
- `400 Bad Request`: invalid `TemplateType` (not a valid enum value)

**Tests:**
- Deleting an existing template returns 204
- Deleting a non-existing template returns 204 (idempotent)
- After reset, GET list returns defaults for that type (IsCustomized=false)
- Invalid TemplateType returns 400

---

### B6 — Preview email template endpoint

**Slice:** `Chairly.Api/Features/Notifications/PreviewEmailTemplate/`

**POST /api/notifications/email-templates/preview:**
- Command: `PreviewEmailTemplateCommand` with:
  - `TemplateType` (string, required)
  - `Subject` (string, required, max 500)
  - `MainMessage` (string, required, max 2000)
  - `ClosingMessage` (string, required, max 1000)
- Handler:
  1. Parse `TemplateType`. If invalid, return `400 Bad Request`.
  2. Replace placeholders in Subject, MainMessage, and ClosingMessage with sample data:
     - `{clientName}` -> "Jan de Vries"
     - `{salonName}` -> tenant's CompanyName from TenantSettings (or "Uw salon") — same cross-context read pattern as B3 (direct DbContext query, not a cross-slice handler call)
     - `{date}` -> formatted current date + time (Dutch format)
     - `{services}` -> "Heren knippen, Baard trimmen"
     - `{invoiceNumber}` -> "F-2026-001"
     - `{invoiceDate}` -> formatted current date (Dutch format)
     - `{totalAmount}` -> formatted EUR 75,00
  3. Render the full HTML email using the existing `EmailTemplates.BuildTemplate()` layout with the replaced values.
  4. Return the rendered HTML and subject.
- Return `200 OK` with:
```csharp
internal sealed record PreviewEmailTemplateResponse(string Subject, string HtmlBody);
```
- Access: Owner and Manager.

**Validation (Data Annotations):**
- `Subject`: `[Required]`, `[MaxLength(500)]`
- `MainMessage`: `[Required]`, `[MaxLength(2000)]`
- `ClosingMessage`: `[Required]`, `[MaxLength(1000)]`

**Error responses:**
- `400 Bad Request`: invalid `TemplateType`
- `422 Unprocessable Entity`: validation failures on body fields

**Implementation note:** The `BuildTemplate` method in `EmailTemplates.cs` is currently `private static`. It needs to be made `internal static` so the preview handler can call it, or extract a shared rendering method. The handler should use the same HTML layout as actual emails.

**Tests:**
- Preview replaces all placeholders with sample data
- Preview uses tenant's CompanyName as salonName
- Preview returns valid HTML with the correct layout
- Invalid TemplateType returns 400
- Empty Subject returns 422

---

### B7 — Update NotificationDispatcher to use custom templates

Modify `NotificationDispatcher.RenderBookingTemplateAsync` and `RenderInvoiceTemplateAsync` to check for a custom `EmailTemplate` row before falling back to the hardcoded defaults in `EmailTemplates.cs`.

**Changes to `NotificationDispatcher.cs`:**

1. In `DispatchSingleNotificationAsync` or `RenderTemplateAsync`, load all `EmailTemplate` rows for the notification's tenant (single query, cache per dispatch cycle if needed).
2. In `RenderBookingTemplateAsync`:
   - Look up custom template for the notification's type.
   - If found: replace placeholders (`{clientName}`, `{salonName}`, `{date}`, `{services}`) in `Subject`, `MainMessage`, `ClosingMessage`, then pass to `EmailTemplates.BuildTemplate()`.
   - If not found: use existing hardcoded `EmailTemplates.BookingConfirmation()` etc. (no change from current behavior).
3. In `RenderInvoiceTemplateAsync`:
   - Same pattern: look up custom template, replace placeholders (`{clientName}`, `{salonName}`, `{invoiceNumber}`, `{invoiceDate}`, `{totalAmount}`), render via `BuildTemplate()`.
   - If not found: use existing `EmailTemplates.InvoiceSent()`.

**Important:** The `EmailTemplates.BuildTemplate()` method must be made accessible (change from `private` to `internal`). Its signature accepts `salonName`, `clientName`, `mainMessage`, `formattedDate`, `serviceSummary`, `closingMessage` — the custom template values (after placeholder replacement) map directly to these parameters.

**Tests:**
- Dispatcher uses custom template when DB row exists
- Dispatcher falls back to defaults when no DB row exists
- Placeholders are correctly replaced in custom templates
- Custom Subject is used for the email subject line

---

## Frontend Tasks

### F1 — Email template models

**Location:** `libs/chairly/src/lib/notifications/models/`

Add email template interfaces to the notifications models folder.

**File: `email-template.model.ts`:**
```typescript
export interface EmailTemplateResponse {
  templateType: string;
  subject: string;
  mainMessage: string;
  closingMessage: string;
  isCustomized: boolean;
  availablePlaceholders: string[];
}

export interface UpdateEmailTemplateRequest {
  subject: string;
  mainMessage: string;
  closingMessage: string;
}

export interface PreviewEmailTemplateRequest {
  templateType: string;
  subject: string;
  mainMessage: string;
  closingMessage: string;
}

export interface PreviewEmailTemplateResponse {
  subject: string;
  htmlBody: string;
}
```

Update the `index.ts` barrel export in the models folder.

---

### F2 — Email template API service methods

**Location:** `libs/chairly/src/lib/notifications/data-access/notifications-api.service.ts`

Add methods to the existing `NotificationsApiService`:

```typescript
getEmailTemplates(): Observable<EmailTemplateResponse[]> {
  return this.http.get<EmailTemplateResponse[]>(`${this.baseUrl}/notifications/email-templates`);
}

updateEmailTemplate(templateType: string, request: UpdateEmailTemplateRequest): Observable<EmailTemplateResponse> {
  return this.http.put<EmailTemplateResponse>(
    `${this.baseUrl}/notifications/email-templates/${templateType}`,
    request
  );
}

resetEmailTemplate(templateType: string): Observable<void> {
  return this.http.delete<void>(`${this.baseUrl}/notifications/email-templates/${templateType}`);
}

previewEmailTemplate(request: PreviewEmailTemplateRequest): Observable<PreviewEmailTemplateResponse> {
  return this.http.post<PreviewEmailTemplateResponse>(
    `${this.baseUrl}/notifications/email-templates/preview`,
    request
  );
}
```

Import the new model types. Update the barrel export `index.ts` in data-access.

---

### F3 — Email template store

**Location:** `libs/chairly/src/lib/notifications/data-access/email-template.store.ts`

Create an NgRx SignalStore for managing email template state.

**State:**
- `templates: EmailTemplateResponse[]`
- `isLoading: boolean`
- `isSaving: boolean`
- `saveError: string | null`
- `saveSuccess: boolean`
- `preview: PreviewEmailTemplateResponse | null`
- `isLoadingPreview: boolean`

**Methods (rxMethod or patchState-based):**
- `loadTemplates()` — calls `getEmailTemplates()`, sets `templates`
- `updateTemplate(templateType: string, request: UpdateEmailTemplateRequest)` — calls `updateEmailTemplate()`, updates the matching item in `templates`
- `resetTemplate(templateType: string)` — calls `resetEmailTemplate()`, then reloads all templates
- `previewTemplate(request: PreviewEmailTemplateRequest)` — calls `previewEmailTemplate()`, sets `preview` and `isLoadingPreview` in state. The component reads `preview` signal to display in the modal.

**Computed signals:**
- `templatesByType` — a computed record/map keyed by `templateType` for easy lookup

Update the barrel export `index.ts` in data-access.

**Vitest test cases (`email-template.store.spec.ts`):**
- `loadTemplates` sets `isLoading` to true while loading and false after completion
- `loadTemplates` populates `templates` with the API response
- `loadTemplates` sets `isLoading` to false and keeps `templates` empty on error
- `updateTemplate` sets `isSaving` to true while saving and false after completion
- `updateTemplate` updates the matching template in the `templates` array
- `updateTemplate` sets `saveSuccess` to true on success
- `updateTemplate` sets `saveError` with error message on failure
- `resetTemplate` reloads all templates after successful reset
- `previewTemplate` sets `isLoadingPreview` to true while loading
- `previewTemplate` populates `preview` with the API response
- `previewTemplate` sets `isLoadingPreview` to false on error
- `templatesByType` computed signal returns templates indexed by type

---

### F4 — Template type label pipe (shared)

**Location:** `libs/shared/src/lib/pipes/template-type-label/`

Create a shared pipe that maps template type strings to Dutch labels. This pipe lives in `shared/` because it is consumed by components in the settings domain, and the notifications domain pipe cannot be imported by settings due to Sheriff module boundary rules.

**File: `template-type-label.pipe.ts`:**
```typescript
import { Pipe, PipeTransform } from '@angular/core';

const templateTypeLabels: Record<string, string> = {
  BookingConfirmation: 'Boekingsbevestiging',
  BookingReminder: 'Boekingsherinnering',
  BookingCancellation: 'Boekingsannulering',
  BookingReceived: 'Boeking ontvangen',
  InvoiceSent: 'Factuur verzonden',
};

@Pipe({
  name: 'templateTypeLabel',
  standalone: true,
})
export class TemplateTypeLabelPipe implements PipeTransform {
  transform(type: string): string {
    return templateTypeLabels[type] ?? type;
  }
}
```

Note: This pipe uses `string` as input type (not `NotificationType` from notifications/models) to avoid a shared -> domain dependency. The fallback returns the raw type string for unknown values.

Update or create the `index.ts` barrel export in `libs/shared/src/lib/pipes/` to include the pipe. Ensure `libs/shared/src/index.ts` re-exports from `./lib/pipes/`.

**Vitest test cases (`template-type-label.pipe.spec.ts`):**
- Transforms `'BookingConfirmation'` to `'Boekingsbevestiging'`
- Transforms `'BookingReminder'` to `'Boekingsherinnering'`
- Transforms `'BookingCancellation'` to `'Boekingsannulering'`
- Transforms `'BookingReceived'` to `'Boeking ontvangen'`
- Transforms `'InvoiceSent'` to `'Factuur verzonden'`
- Returns the raw input string for unknown type values

---

### F5 — Email templates list page (smart component)

**Location:** `libs/chairly/src/lib/settings/feature/email-templates-page/`

This is the main page showing all 5 email templates with their current status.

**Route:** `/instellingen/email-templates` — add as a child route in `settings.routes.ts`.

**Component: `EmailTemplatesPageComponent`** (smart, standalone, OnPush)
- Selector: `chairly-email-templates-page`
- Injects `EmailTemplateStore`
- On init: calls `store.loadTemplates()`
- Displays a list/card layout of all 5 template types

**Template layout:**
- Page header: "E-mailtemplates"
- Description: "Pas de e-mails aan die naar uw klanten worden verstuurd. Bewerk het onderwerp, het bericht en de afsluiting per type e-mail."
- For each template, display a card with:
  - Template type label (Dutch, using the `TemplateTypeLabelPipe` from shared)
  - Current subject line (truncated if long)
  - Badge: "Aangepast" (green) if `isCustomized`, "Standaard" (gray) if not
  - "Bewerken" button -> navigates to edit page
- Loading state with `LoadingIndicatorComponent`

**Note on navigation:** Clicking "Bewerken" navigates to a sub-route `/instellingen/email-templates/{templateType}` which loads the edit component (F6).

**Register route in `settings.routes.ts`:**
```typescript
{
  path: 'email-templates',
  loadComponent: () =>
    import('./feature/email-templates-page/email-templates-page.component').then(
      (m) => m.EmailTemplatesPageComponent,
    ),
},
{
  path: 'email-templates/:templateType',
  loadComponent: () =>
    import('./feature/email-template-edit-page/email-template-edit-page.component').then(
      (m) => m.EmailTemplateEditPageComponent,
    ),
},
```

**Sidebar nav:** Add "E-mailtemplates" as a sub-item under "Instellingen" in the sidebar, linking to `/instellingen/email-templates`.

---

### F6 — Email template edit page (smart component)

**Location:** `libs/chairly/src/lib/settings/feature/email-template-edit-page/`

**Component: `EmailTemplateEditPageComponent`** (smart, standalone, OnPush)
- Selector: `chairly-email-template-edit-page`
- Reads `templateType` from route params
- Injects `EmailTemplateStore`, `Router`, `DestroyRef`
- Does NOT inject `NotificationsApiService` directly — all API interactions go through the store
- On init: loads templates (if not already loaded), finds the template matching the route param

**Template layout:**
- Page header: template type label in Dutch (e.g. "Boekingsbevestiging bewerken")
- Back link: "Terug naar overzicht" -> `/instellingen/email-templates`
- Reactive form with 3 fields:
  - "Onderwerp" (Subject) — `<input type="text">`, max 500 chars
  - "Hoofdbericht" (MainMessage) — `<textarea>`, max 2000 chars
  - "Afsluitbericht" (ClosingMessage) — `<textarea>`, max 1000 chars
- Below each field: hint text showing available placeholders for this template type, e.g. "Beschikbare variabelen: {clientName}, {salonName}, {date}, {services}"
- Button row:
  - "Opslaan" (primary) — saves the template via `store.updateTemplate()`
  - "Voorbeeld bekijken" (secondary) — calls `store.previewTemplate()` and opens the preview modal (F7) when the preview signal is populated
  - "Standaardwaarden herstellen" (danger/outline) — confirm dialog (using shared `ConfirmationDialogComponent`), then calls `store.resetTemplate()`, navigates back to list
- Success banner: "Template opgeslagen"
- Error banner: display error message
- Dark mode: ensure all form fields, cards, and backgrounds have proper `dark:` variants

---

### F7 — Email preview modal (presentational component)

**Location:** `libs/chairly/src/lib/settings/ui/email-preview-modal/`

**Component: `EmailPreviewModalComponent`** (presentational, standalone, OnPush)
- Selector: `chairly-email-preview-modal`
- Inputs (signal-based):
  - `subject: input<string>()` — the rendered subject line
  - `htmlBody: input<string>()` — the rendered HTML body
- Uses native `<dialog>` pattern per CLAUDE.md conventions (full-screen overlay with `showModal()`)
- Injects `DOCUMENT` for body overflow management

**Template layout:**
- Dialog title: "Voorbeeld e-mail"
- Subject display: bold text showing the rendered subject
- Email body: rendered using `<iframe [attr.srcdoc]="htmlBody()">` — this avoids Angular sanitization concerns entirely and provides natural email rendering isolation. Do NOT use `[innerHTML]`.
- "Sluiten" button to close the dialog
- Dialog width: `max-w-2xl` to show the email at a readable width

**Public methods:**
- `open()` — calls `this.dialogRef().showModal()`, sets body overflow hidden
- `close()` — closes dialog, restores body overflow

---

### F8 — Playwright e2e tests

**Location:** `apps/chairly-e2e/src/email-templates.spec.ts`

**Test scenarios:**
1. Navigate to `/instellingen/email-templates` — verify page heading "E-mailtemplates" is visible
2. Verify all 5 template cards are displayed with correct Dutch labels
3. Verify default templates show "Standaard" badge
4. Click "Bewerken" on a template — verify edit page loads with pre-filled default values
5. Edit the subject field, click "Opslaan" — verify success banner appears
6. Navigate back to list — verify template now shows "Aangepast" badge
7. Click "Bewerken" on the customized template — verify it shows the custom values
8. Click "Voorbeeld bekijken" — verify preview modal opens with rendered HTML in an iframe
9. Close preview modal (Escape key per CLAUDE.md convention)
10. Click "Standaardwaarden herstellen" — confirm — verify template resets to "Standaard" badge

---

## Acceptance Criteria

- [ ] `EmailTemplate` entity exists in `Chairly.Domain` with `Subject`, `MainMessage`, `ClosingMessage` fields
- [ ] EF configuration with unique index on `(TenantId, TemplateType)`
- [ ] `DbSet<EmailTemplate>` added to `ChairlyDbContext`
- [ ] Idempotent migration for `EmailTemplates` table
- [ ] `docs/domain-model.md` updated with all 5 `NotificationType` values and `EmailTemplate` entity
- [ ] `DefaultEmailTemplateValues` helper returns correct defaults and placeholders for all 5 types
- [ ] `GET /api/notifications/email-templates` returns 5 templates (custom or default)
- [ ] `PUT /api/notifications/email-templates/{templateType}` creates/updates custom template with correct AvailablePlaceholders in response
- [ ] `DELETE /api/notifications/email-templates/{templateType}` resets to default (deletes row)
- [ ] `POST /api/notifications/email-templates/preview` returns rendered HTML with sample data
- [ ] Invalid `TemplateType` in route returns `400 Bad Request` (not 422)
- [ ] `NotificationDispatcher` uses custom templates when they exist, falls back to defaults
- [ ] Frontend email template models match backend DTOs
- [ ] API service methods added to `NotificationsApiService`
- [ ] NgRx SignalStore manages template state including preview
- [ ] Store has Vitest unit tests for loading, saving, error handling, and preview
- [ ] Template type label pipe lives in `shared/src/lib/pipes/` with Vitest unit tests
- [ ] Email templates list page at `/instellingen/email-templates`
- [ ] Email template edit page interacts with store only (no direct service injection)
- [ ] Preview modal uses `<iframe srcdoc>` for HTML rendering (not innerHTML)
- [ ] "Standaardwaarden herstellen" deletes DB row and shows defaults
- [ ] Owner and Manager can access; Staff Member cannot
- [ ] All user-facing text is Dutch
- [ ] All backend quality checks pass (dotnet build, test, format)
- [ ] All frontend quality checks pass (lint, test, build)
- [ ] Playwright e2e tests pass

## Out of Scope

- Full HTML layout customization (only structured fields are editable)
- Rich text / WYSIWYG editor for message fields
- Custom placeholder creation by tenants
- Email template versioning or audit history
- SMS template customization
- Localization / multi-language template support
- Per-staff-member template overrides
- Sending test emails to a real address
- Template inheritance or template composition
