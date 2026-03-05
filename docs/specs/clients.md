# Feature: Client Management

## Context

Salons need to maintain a client database to track who visits, their contact information, and any notes about their preferences. Clients are a core dependency of Bookings — a booking always belongs to a client. This feature provides full CRUD management for clients, with soft-delete to preserve booking history.

## User Stories

- As an owner, manager, or staff member, I want to view a list of clients so that I can find and manage client information.
- As an owner, manager, or staff member, I want to add a new client so that I can record their contact information before or during a booking.
- As an owner, manager, or staff member, I want to edit a client's information so that I can keep it accurate over time.
- As an owner or manager, I want to delete a client so that I can remove outdated records, while preserving their booking history.

## Acceptance Criteria

- [ ] GET /api/clients returns all non-deleted clients for the current tenant
- [ ] POST /api/clients creates a new client with required name and optional contact info
- [ ] PUT /api/clients/{id} updates an existing client's information
- [ ] DELETE /api/clients/{id} soft-deletes a client (sets DeletedAtUtc/DeletedBy)
- [ ] Deleted clients are excluded from the list response
- [ ] Deleted clients remain in the database (preserves booking history)
- [ ] A client with only first/last name and no contact info can be created (email and phone are optional)
- [ ] Client list page at /klanten shows all active clients in a table
- [ ] Form dialog supports both create and edit mode in Dutch
- [ ] Soft-deleted clients cannot be edited or re-deleted
- [ ] Nav item 'Klanten' is present in the sidebar between 'Diensten' and 'Medewerkers'

## Domain Model

**Entity: Client (Aggregate Root)**

| Field | Type | Notes |
|---|---|---|
| `Id` | Guid | PK |
| `TenantId` | Guid | FK — all queries filter by this |
| `FirstName` | string | required, max 100 |
| `LastName` | string | required, max 100 |
| `Email` | string? | optional, valid email format if provided |
| `PhoneNumber` | string? | optional |
| `Notes` | string? | optional, max 1000 chars |
| `CreatedAtUtc` | DateTimeOffset | set on create |
| `CreatedBy` | Guid | set on create |
| `UpdatedAtUtc` | DateTimeOffset? | set on update |
| `UpdatedBy` | Guid? | set on update |
| `DeletedAtUtc` | DateTimeOffset? | set on soft-delete |
| `DeletedBy` | Guid? | set on soft-delete |

**Derived state (no status column — per ADR-009):**
- **Active**: `DeletedAtUtc` is null
- **Deleted**: `DeletedAtUtc` is set

## API Endpoints

| Method | Route | Description |
|--------|-------|-------------|
| GET | /api/clients | List all non-deleted clients for tenant |
| POST | /api/clients | Create a new client |
| PUT | /api/clients/{id} | Update an existing client |
| DELETE | /api/clients/{id} | Soft-delete a client |

### GET /api/clients

Returns `ClientResponse[]` sorted by `LastName` then `FirstName`.

### POST /api/clients

Request body:
```json
{
  "firstName": "string (required, max 100)",
  "lastName": "string (required, max 100)",
  "email": "string | null (optional, valid email)",
  "phoneNumber": "string | null (optional)",
  "notes": "string | null (optional, max 1000)"
}
```

Returns `201 Created` with the created `ClientResponse`.

### PUT /api/clients/{id}

Same request body as POST. Returns `200 OK` with updated `ClientResponse`.

Returns `404 Not Found` if client does not exist or belongs to a different tenant.

### DELETE /api/clients/{id}

No request body. Sets `DeletedAtUtc` and `DeletedBy`.

Returns `204 No Content` on success.
Returns `404 Not Found` if client does not exist or belongs to a different tenant.
Returns `409 Conflict` (idempotency) if client is already deleted.

### ClientResponse

```json
{
  "id": "guid",
  "firstName": "string",
  "lastName": "string",
  "email": "string | null",
  "phoneNumber": "string | null",
  "notes": "string | null",
  "createdAtUtc": "ISO 8601",
  "updatedAtUtc": "ISO 8601 | null"
}
```

## Business Rules

- A client belongs to exactly one tenant
- Email, if provided, must be a valid email address format
- Soft-deleted clients are excluded from all list queries
- Deletion is idempotent: re-deleting an already-deleted client returns 409
- Deleting a client does NOT delete their bookings
- No deduplication — same name can appear multiple times per tenant
- `CreatedBy` and `UpdatedBy`/`DeletedBy` are the authenticated user ID (Guid); use `Guid.Empty` until real auth middleware is in place

## UI/UX

**Page: /klanten (Klanten)**

- Page header: `<h1>Klanten</h1>` with a `+ Klant toevoegen` button (bg-primary-600)
- Loading state: shows 'Laden...' while fetching
- Empty state: 'Geen klanten gevonden'
- Table columns: Naam (lastName, firstName), E-mailadres, Telefoonnummer, Acties
- Actions per row: 'Bewerken' button, 'Verwijderen' button (red, with confirmation dialog)
- Error message (Dutch) shown below page header when an API mutation fails

**Form dialog (create/edit):**

- Title: 'Klant toevoegen' (create) / 'Klant bewerken' (edit)
- Fields (Dutch labels):
  - Voornaam (required)
  - Achternaam (required)
  - E-mailadres (optional, email input)
  - Telefoonnummer (optional, tel input)
  - Notities (optional, textarea)
- Buttons: 'Opslaan' (disabled when invalid), 'Annuleren'
- Full-screen overlay `<dialog>` pattern (same as staff/services)

**Nav sidebar:**

- 'Klanten' link at /klanten, inserted between 'Diensten' and 'Medewerkers'

## Events (async)

None for this feature. Client events (e.g. `ClientDeleted`) may be added in a future iteration when Notifications is implemented.

## Out of Scope

- Client search / filtering (future feature)
- Client merge (future feature)
- Import from CSV (future feature)
- Client portal / self-service (out of scope for this product)
- Re-activating a deleted client (deleted = gone from UI; history preserved in DB only)
