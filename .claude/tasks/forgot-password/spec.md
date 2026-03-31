# Forgot Password

> **Status: Implemented** — Merged to main.

## Overview

This feature enables password reset for staff members through two flows:

1. **Self-service**: Staff members can reset their own password from the Keycloak login page via the built-in "Forgot password?" link. This requires enabling `resetPasswordAllowed` on the Keycloak realm.
2. **Manager-triggered**: Owners and Managers can trigger a password reset email for any staff member from within the Chairly application. This calls the Keycloak Admin API to send an `UPDATE_PASSWORD` actions email.

Both flows rely on Keycloak's email integration, which is already configured via MailDev in local development (see `KeycloakDevSeeder.UpdateRealmSettingsAsync` and AppHost SMTP wiring).

## Domain Context

- Bounded context: Staff (cross-cutting with Identity/Keycloak infrastructure)
- Key entities involved: `StaffMember` (has `KeycloakUserId` linking to the Keycloak user)
- Ubiquitous language: Staff Member, Owner, Manager, Tenant

**Note on field naming:** The domain model document (`docs/domain-model.md`) lists the field as `UserId`, but the actual `StaffMember` entity in code (`Chairly.Domain/Entities/StaffMember.cs`) uses `KeycloakUserId` (type `string?`). This spec uses the code-level name `KeycloakUserId` throughout since that is what implementers will encounter.

## Infrastructure Tasks

### I1 — Enable "Forgot password" on Keycloak realm

Enable the self-service password reset flow on Keycloak realms so the login page shows a "Forgot password?" link.

**Changes to `CreateRealmAsync` in `KeycloakAdminService`:**

Add `resetPasswordAllowed = true` to the realm representation object in the `CreateRealmAsync` method. This ensures all newly provisioned tenant realms have self-service password reset enabled from the start.

**Changes to `KeycloakDevSeeder`:**

In the `CreateRealmAsync` method of `KeycloakDevSeeder`, add `resetPasswordAllowed = true` to the realm object.

In the `UpdateRealmSettingsAsync` method, add `resetPasswordAllowed = true` to the settings object that is PUT to the realm. This ensures existing dev realms (already created before this change) also get the setting enabled.

Similarly, add `resetPasswordAllowed = true` to the admin realm creation in `CreateAdminRealmAsync` for consistency.

**Files to modify:**
- `src/backend/Chairly.Infrastructure/Keycloak/KeycloakAdminService.cs` — add `resetPasswordAllowed = true` to `CreateRealmAsync` realm representation
- `src/backend/Chairly.Api/Shared/Tenancy/KeycloakDevSeeder.cs` — add `resetPasswordAllowed = true` to:
  - `CreateRealmAsync` realm object
  - `UpdateRealmSettingsAsync` settings object
  - `CreateAdminRealmAsync` realm object

**Verification:**
- After restarting the dev environment, navigate to the Keycloak login page for the tenant realm
- A "Forgot password?" link should be visible below the login form
- Clicking it should prompt for an email address
- Submitting the email should send a password reset email visible in MailDev (http://localhost:1080)

**Test cases:**
- Existing `KeycloakAdminServiceTests` should still pass (no behavioral change, just an added field)
- Manual verification that the login page shows the link

## Backend Tasks

### B1 — Reset password endpoint for manager-triggered flow

Create a new vertical slice `ResetStaffPassword` under `Features/Staff/` that allows Owners and Managers to trigger a password reset email for a staff member.

> **Note:** I1 (realm config) is a deployment prerequisite, not a code dependency. B1 can be implemented independently.

**Command:**
```csharp
// File: Features/Staff/ResetStaffPassword/ResetStaffPasswordCommand.cs
internal sealed record ResetStaffPasswordCommand(Guid Id) : IRequest<OneOf<Success, NotFound>>;
```

The command takes the staff member's `Id`. No additional fields needed — the handler resolves the Keycloak user and sends the actions email.

**Handler:**
```csharp
// File: Features/Staff/ResetStaffPassword/ResetStaffPasswordHandler.cs
```

Handler logic:
1. Look up the `StaffMember` by `Id` and `TenantId` (from `ITenantContext`)
2. If not found, return `NotFound`
3. If `member.KeycloakUserId` is null, return `NotFound` (cannot reset password for a user not linked to Keycloak)
4. Call `IKeycloakAdminService.SendActionsEmailAsync(tenantContext.TenantId, member.KeycloakUserId, ["UPDATE_PASSWORD"], ct)`
5. Return `Success`
6. If the Keycloak call fails (`HttpRequestException` or `InvalidOperationException`), log a warning and still return `Success` — the UI should not expose Keycloak internal errors. Use the `partial LoggerMessage` pattern consistent with `DeactivateStaffMemberHandler`:

```csharp
[LoggerMessage(Level = LogLevel.Warning, Message = "Failed to send password reset email for staff member {StaffMemberId}; Keycloak may be unreachable")]
private static partial void LogKeycloakResetFailed(ILogger logger, Guid staffMemberId, Exception exception);
```

**Endpoint:**
```csharp
// File: Features/Staff/ResetStaffPassword/ResetStaffPasswordEndpoint.cs
```

- Route: `POST /api/staff/{id:guid}/reset-password` (group-relative: `POST /{id:guid}/reset-password`, registered on `writeGroup` which has base path `/api/staff`)
- Authorization: `RequireManager` policy (Owner + Manager can access)
- Response:
  - `200 OK` with empty body on success (the `Success` type is matched to `Results.Ok()` with no payload)
  - `404 Not Found` with empty body if staff member does not exist or has no `KeycloakUserId`

Example:
```csharp
return result.Match(
    _ => Results.Ok(),
    _ => Results.NotFound());
```

**Files to create:**
- `src/backend/Chairly.Api/Features/Staff/ResetStaffPassword/ResetStaffPasswordCommand.cs`
- `src/backend/Chairly.Api/Features/Staff/ResetStaffPassword/ResetStaffPasswordHandler.cs`
- `src/backend/Chairly.Api/Features/Staff/ResetStaffPassword/ResetStaffPasswordEndpoint.cs`

**Files to modify:**
- `src/backend/Chairly.Api/Features/Staff/StaffEndpoints.cs` — add `using` for the new namespace, register `writeGroup.MapResetStaffPassword()`

**Test cases:**
- Unit test: handler returns `Success` when staff member exists and has a `KeycloakUserId`
- Unit test: handler returns `NotFound` when staff member does not exist
- Unit test: handler returns `NotFound` when staff member has no `KeycloakUserId`
- Unit test: handler returns `Success` even when Keycloak call throws `HttpRequestException` (graceful degradation)
- Integration test: `POST /api/staff/{id}/reset-password` returns 200 for valid staff member
- Integration test: `POST /api/staff/{id}/reset-password` returns 404 for non-existent ID
- Integration test: `POST /api/staff/{id}/reset-password` returns `403 Forbidden` when called by a staff member (non-manager) role

## Frontend Tasks

### F1 — Add reset password method to staff API service

Add a `resetPassword` method to `StaffApiService`. No store changes are needed — the reset password action is fire-and-forget from a state perspective. The component will call the API service directly (similar to how deactivate/reactivate work in `StaffListPageComponent`).

**API Service (`staff-api.service.ts`):**
```typescript
resetPassword(id: string): Observable<void> {
  return this.http.post<void>(`${this.baseUrl}/staff/${id}/reset-password`, null);
}
```

**Files to modify:**
- `src/frontend/chairly/libs/chairly/src/lib/staff/data-access/staff-api.service.ts` — add `resetPassword` method

**Test cases:**
- Unit test: `StaffApiService.resetPassword` calls `POST /api/staff/{id}/reset-password`

### F2 — Add reset password button to staff table and form dialog

Add a "Reset wachtwoord" action button in two locations:
1. The staff table row actions (next to "Bewerken" and "Deactiveren/Activeren")
2. The staff form dialog (edit mode only, in the action buttons area)

Both trigger the same flow: show a confirmation dialog, then call the API, then show a success message.

**Staff table (`staff-table.component.html` and `.ts`):**

Add a new output:
```typescript
readonly resetPassword: OutputEmitterRef<StaffMemberResponse> = output<StaffMemberResponse>();
```

Add a button in the actions column, between "Bewerken" and "Deactiveren"/"Activeren":
```html
<button
  type="button"
  class="inline-flex items-center rounded border border-gray-200 bg-gray-50 px-2 py-1 text-xs font-medium text-gray-700 transition-colors hover:border-gray-300 hover:bg-gray-100 active:bg-gray-200 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-gray-500 focus-visible:ring-offset-1 dark:bg-gray-700 dark:border-gray-600 dark:text-white dark:hover:bg-gray-600 dark:hover:border-gray-500 dark:active:bg-gray-800"
  title="Wachtwoord resetten"
  (click)="resetPassword.emit(member)">
  Reset wachtwoord
</button>
```

Only show this button when the member is active (`@if (member.isActive)`).

**Staff form dialog (`staff-form-dialog.component.html` and `.ts`):**

Add a new output:
```typescript
readonly resetPassword: OutputEmitterRef<void> = output<void>();
```

In edit mode only (when `staffMember()` is truthy), add a "Reset wachtwoord" link/button in the dialog footer, left-aligned (before the Annuleren/Opslaan buttons which are right-aligned):
```html
@if (staffMember(); as member) {
  @if (member.isActive) {
    <button
      type="button"
      class="text-sm text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-200 underline"
      (click)="resetPassword.emit()">
      Reset wachtwoord
    </button>
  }
}
```

Adjust the footer flex layout so the reset button is on the left and Annuleren/Opslaan stay on the right (use `justify-between` on the parent if the reset button is present).

**Files to modify:**
- `src/frontend/chairly/libs/chairly/src/lib/staff/ui/staff-table/staff-table.component.ts` — add `resetPassword` output
- `src/frontend/chairly/libs/chairly/src/lib/staff/ui/staff-table/staff-table.component.html` — add button
- `src/frontend/chairly/libs/chairly/src/lib/staff/ui/staff-form-dialog/staff-form-dialog.component.ts` — add `resetPassword` output
- `src/frontend/chairly/libs/chairly/src/lib/staff/ui/staff-form-dialog/staff-form-dialog.component.html` — add button in footer

**Test cases:**
- Unit test: StaffTableComponent emits `resetPassword` event when button is clicked
- Unit test: StaffFormDialogComponent emits `resetPassword` event when link is clicked (edit mode)
- Unit test: Reset password button is not rendered when member is inactive (table)
- Unit test: Reset password link is not rendered in add mode (dialog)
- Unit test: Reset password link is not rendered when member is inactive (dialog)

### F3 — Wire up reset password flow in staff list page

Connect the reset password buttons to the API via a confirmation dialog and success feedback.

**Confirmation dialog:**

Add a new `ConfirmationDialogComponent` instance to the staff list page template:
```html
<chairly-confirmation-dialog
  #resetPasswordDialog
  title="Wachtwoord resetten"
  message="Weet je zeker dat je een wachtwoord-reset e-mail wilt versturen naar deze medewerker?"
  confirmLabel="Versturen"
  cancelLabel="Annuleren"
  (confirmed)="onConfirmResetPassword()" />
```

**Success feedback:**

Since there is no toast service in the application yet, use a temporary success banner/message displayed at the top of the page (similar to how `mutationError` is already used for error states). Add a `resetPasswordSuccess` signal:
```typescript
protected readonly resetPasswordSuccess = signal<string | null>(null);
```

Display it in the template as a green info banner when set:
```html
@if (resetPasswordSuccess()) {
  <div class="mb-4 rounded-md bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 p-3 text-sm text-green-700 dark:text-green-300">
    {{ resetPasswordSuccess() }}
  </div>
}
```

Auto-clear the success message after 5 seconds using an RxJS `timer` piped through `takeUntilDestroyed(destroyRef)` to ensure proper cleanup if the component is destroyed before the timer fires.

**Component logic (`staff-list-page.component.ts`):**

```typescript
private readonly destroyRef = inject(DestroyRef);

private readonly resetPasswordDialogRef =
  viewChild.required<ConfirmationDialogComponent>('resetPasswordDialog');

protected readonly resetPasswordSuccess = signal<string | null>(null);

protected onResetPassword(member: StaffMemberResponse, fromFormDialog = false): void {
  this.selectedStaff.set(member);
  this.resetPasswordSuccess.set(null);
  // If triggered from form dialog, close it first before opening confirmation
  if (fromFormDialog) {
    this.formDialogRef().close();
  }
  this.resetPasswordDialogRef().open();
}

protected onConfirmResetPassword(): void {
  const member = this.selectedStaff();
  if (!member) return;

  this.staffApi
    .resetPassword(member.id)
    .pipe(takeUntilDestroyed(this.destroyRef))
    .subscribe({
      next: () => {
        const message = `Wachtwoord-reset e-mail is verstuurd naar ${member.firstName} ${member.lastName}.`;
        this.resetPasswordSuccess.set(message);
        this.selectedStaff.set(null);
        // Auto-clear after 5 seconds using RxJS timer + takeUntilDestroyed
        timer(5000)
          .pipe(takeUntilDestroyed(this.destroyRef))
          .subscribe(() => {
            if (this.resetPasswordSuccess() === message) {
              this.resetPasswordSuccess.set(null);
            }
          });
      },
      error: () => {
        this.mutationError.set('Er is een fout opgetreden bij het versturen van de wachtwoord-reset e-mail.');
        this.selectedStaff.set(null);
      },
    });
}
```

**Required imports:** Add `timer` from `rxjs` and ensure `takeUntilDestroyed` from `@angular/core/rxjs-interop` and `DestroyRef` from `@angular/core` are imported. The component likely already injects `DestroyRef` — reuse the existing injection.

**Template wiring:**

Wire the `resetPassword` events from both the table and the form dialog:
- Table: `(resetPassword)="onResetPassword($event)"`
- Form dialog: `(resetPassword)="onResetPassword(selectedStaff()!, true)"` — when triggered from the edit dialog, the `selectedStaff` signal already holds the member being edited. The `fromFormDialog` flag ensures the form dialog is closed before opening the confirmation dialog.

**Files to modify:**
- `src/frontend/chairly/libs/chairly/src/lib/staff/feature/staff-list-page/staff-list-page.component.ts` — add reset password logic, import `timer` from `rxjs`
- `src/frontend/chairly/libs/chairly/src/lib/staff/feature/staff-list-page/staff-list-page.component.html` — add confirmation dialog, success banner, wire events

**Test cases:**
- Unit test: Clicking reset password on table row opens confirmation dialog
- Unit test: Confirming the dialog calls `StaffApiService.resetPassword`
- Unit test: Success response shows success banner with staff member name
- Unit test: Error response shows error message
- Unit test: Success banner auto-clears after timeout (use `fakeAsync`/`tick` to advance the RxJS timer)

### F4 — Playwright e2e tests for reset password flow

Add end-to-end tests covering the manager-triggered password reset flow.

**Test file:** `apps/chairly-e2e/src/staff-reset-password.spec.ts`

**Scenarios:**
1. **Manager can trigger password reset from table**: Log in as manager (via the mock Keycloak fixture in `fixtures.ts`), navigate to `/medewerkers` (staff list), click "Reset wachtwoord" on a staff member row, confirm the dialog, verify success banner appears with "Wachtwoord-reset e-mail is verstuurd naar ..."
2. **Manager can trigger password reset from edit dialog**: Log in as manager, click "Bewerken" on a staff member, click "Reset wachtwoord" link in dialog, confirm, verify success banner appears
3. **Confirmation dialog can be cancelled**: Open the reset password confirmation dialog, press `Escape` (using `page.keyboard.press('Escape')` per CLAUDE.md dialog pattern), verify no API call is made and no success banner appears

**Note on self-service login page test:** The e2e test infrastructure uses a fully mocked Keycloak (see `fixtures.ts` — all Keycloak endpoints are intercepted by Playwright route handlers). There is no real Keycloak login page rendered in e2e. Therefore, verifying the "Forgot password?" link on the Keycloak login page is **not feasible in Playwright e2e** and should be verified manually during I1 verification (see I1 verification steps). Do not include a Playwright scenario for this.

**API mock setup:** Add a Playwright route handler for `POST /api/staff/*/reset-password` that returns `200 OK` with an empty body, consistent with the B1 response shape.

**Files to create:**
- `src/frontend/chairly/apps/chairly-e2e/src/staff-reset-password.spec.ts`

## Acceptance Criteria

- [ ] Keycloak login page shows "Forgot password?" link for tenant realms (manual verification)
- [ ] Clicking "Forgot password?" and submitting an email sends a reset email (visible in MailDev)
- [ ] Owners and Managers can trigger a password reset email from the staff table row actions
- [ ] Owners and Managers can trigger a password reset email from the staff edit dialog
- [ ] A confirmation dialog ("Weet je zeker...?") is shown before sending the email
- [ ] A success banner is shown after the reset password request succeeds
- [ ] When Keycloak is unreachable, the endpoint still returns 200 OK (graceful degradation) and the user sees the same success banner — no error is exposed to the UI
- [ ] The reset password button is only shown for active staff members (both the table row button and the edit dialog link)
- [ ] The reset password link in the edit dialog is only shown in edit mode (not add mode) and only for active members
- [ ] Staff members (non-manager role) cannot access the reset password endpoint (403)
- [ ] All backend quality checks pass (dotnet build, test, format)
- [ ] All frontend quality checks pass (lint, test, build)
- [ ] Playwright e2e tests pass

## Out of Scope

- Custom Keycloak email templates (uses Keycloak default templates)
- Toast/snackbar notification system (uses inline success banner as interim solution)
- Staff member resetting their own password from within the Chairly app (they use the Keycloak login page flow)
- Password policy configuration (uses Keycloak realm defaults)
- Audit logging of password reset actions
- Playwright e2e test for the Keycloak login page "Forgot password?" link (not feasible with mocked Keycloak; verified manually)
