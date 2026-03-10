# Recipes (Behandelrecords)

## Overview

Employees can add a **Recipe** (behandelrecord) to a completed booking. A recipe records what was done to the client's hair — which products were used, what colors or techniques were applied, and any freetext notes. Recipes accumulate as a treatment history on the client, so the staff member assigned to the next booking can look up what was done previously without having to ask the client again.

This feature belongs to the **Clients** bounded context, because recipes are client-centric historical records that outlive individual bookings.

---

## Domain Context

- **Bounded context:** Clients
- **Key entities involved:** `Recipe` (Aggregate Root), `RecipeProduct` (Value Object), `Client`, `Booking`
- **Ubiquitous language:**
  - **Recipe** — a treatment record attached to a completed booking, describing what was done to the client's hair (products, colors, techniques)
  - **RecipeProduct** — a single product entry within a recipe (name, brand, quantity/dosage)

### New entities

**`Recipe`** (Aggregate Root)

| Field | Type | Notes |
|---|---|---|
| `Id` | Guid | PK |
| `TenantId` | Guid | FK, multi-tenant scope |
| `BookingId` | Guid | FK → Booking; unique per tenant (one recipe per booking) |
| `ClientId` | Guid | FK → Client (denormalized for efficient history queries) |
| `StaffMemberId` | Guid | The staff member who performed the treatment |
| `Title` | string | Short label (e.g. "Volledige kleuring") |
| `Notes` | string? | Freetext observations |
| `Products` | List\<RecipeProduct\> | Structured product list (owned collection) |
| `CreatedAtUtc` | DateTimeOffset | Required |
| `CreatedBy` | Guid | Required |
| `UpdatedAtUtc` | DateTimeOffset? | Set on every edit |
| `UpdatedBy` | Guid? | Set on every edit |

**`RecipeProduct`** (Value Object / owned entity)

| Field | Type | Notes |
|---|---|---|
| `Id` | Guid | PK on owned table |
| `Name` | string | Product name (e.g. "Wella Illumina") |
| `Brand` | string? | Brand name |
| `Quantity` | string? | Free-form quantity/dosage (e.g. "60 ml", "1:2") |
| `SortOrder` | int | Display order |

### Business rules

- A booking can have **at most one** recipe. Creating a recipe for a booking that already has one returns a conflict error.
- A recipe can only be created or edited when the booking's `CompletedAtUtc` is set (booking must be completed).
- **StaffMember** role: can only create/edit a recipe if `StaffMemberId` on the booking matches their own staff ID.
- **Owner / Manager** role: can create/edit recipes for any booking in the tenant.
- All roles can view recipes (own only for StaffMember, all for Owner/Manager).
- `ClientId` is copied from the booking at creation time (not provided by the caller).
- `StaffMemberId` is copied from the booking at creation time (not provided by the caller).

---

## Backend Tasks

### B1 — Recipe entity, EF configuration, and migration

Create the `Recipe` aggregate root and `RecipeProduct` owned entity in **Chairly.Domain**, wire up EF Core configuration in **Chairly.Infrastructure**, and generate a migration.

**Domain — `Chairly.Domain/Entities/Recipe.cs`:**

```csharp
public class Recipe
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid BookingId { get; set; }
    public Guid ClientId { get; set; }
    public Guid StaffMemberId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public List<RecipeProduct> Products { get; set; } = [];
    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid? UpdatedBy { get; set; }
}
```

**Domain — `Chairly.Domain/Entities/RecipeProduct.cs`:**

```csharp
public class RecipeProduct
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Brand { get; set; }
    public string? Quantity { get; set; }
    public int SortOrder { get; set; }
}
```

**EF Configuration — `Chairly.Infrastructure/Configurations/RecipeConfiguration.cs`:**

- Table: `Recipes`
- Unique index on `(TenantId, BookingId)` — one recipe per booking per tenant
- Index on `(TenantId, ClientId)` — for fast history queries
- `Products` configured as `OwnsMany` → table `RecipeProducts` with FK `RecipeId`
- `Title` max length 200, required
- `Notes` max length 2000

**Migration:** Add and apply migration `AddRecipes`.

---

### B2 — Create recipe endpoint

**Slice:** `Chairly.Api/Features/Clients/CreateRecipe/`

**Route:** `POST /api/recipes`

**Request body:**

```json
{
  "bookingId": "guid",
  "title": "string",
  "notes": "string?",
  "products": [
    { "name": "string", "brand": "string?", "quantity": "string?", "sortOrder": 0 }
  ]
}
```

**Handler logic:**

1. Load the booking by `bookingId` (scoped to `TenantId`). Return `404` if not found.
2. Verify `booking.CompletedAtUtc != null`. Return `422` with error `"Booking is niet afgerond"` if not.
3. Check no recipe exists yet for this booking. Return `409` with error `"Er bestaat al een recept voor deze boeking"` if one does.
4. Authorisation check:
   - If caller's role is `StaffMember`, verify `booking.StaffMemberId == currentUserId`. Return `403` if not.
5. Build `Recipe` from booking (`ClientId`, `StaffMemberId` copied from booking). Set `CreatedAtUtc = UtcNow`, `CreatedBy = currentUserId`.
6. Persist and return `201 Created` with the created recipe as response body.

**Response body:** full recipe (same shape as Get endpoint — see B3).

**Validation:**
- `BookingId` required
- `Title` required, max 200 chars
- `Notes` max 2000 chars
- Each product: `Name` required, max 100 chars; `Brand` max 100 chars; `Quantity` max 50 chars

**Tests:**
- Returns 201 with recipe on happy path
- Returns 404 when booking not found
- Returns 422 when booking not completed
- Returns 409 when recipe already exists for booking
- Returns 403 when StaffMember tries to add recipe for another staff member's booking
- Title validation (empty, too long)

---

### B3 — Get recipe by booking endpoint

**Slice:** `Chairly.Api/Features/Clients/GetRecipeByBooking/`

**Route:** `GET /api/recipes/booking/{bookingId}`

**Handler logic:**

1. Load recipe by `bookingId` scoped to `TenantId`. Return `404` if not found.
2. Authorisation check:
   - If caller's role is `StaffMember`, verify `recipe.StaffMemberId == currentUserId`. Return `403` if not.
3. Return `200 OK` with recipe.

**Response body:**

```json
{
  "id": "guid",
  "bookingId": "guid",
  "clientId": "guid",
  "staffMemberId": "guid",
  "title": "string",
  "notes": "string?",
  "products": [
    { "id": "guid", "name": "string", "brand": "string?", "quantity": "string?", "sortOrder": 0 }
  ],
  "createdAtUtc": "datetime",
  "createdBy": "guid",
  "updatedAtUtc": "datetime?",
  "updatedBy": "guid?"
}
```

**Tests:**
- Returns 200 with recipe on happy path
- Returns 404 when no recipe for booking
- Returns 403 when StaffMember requests another staff member's recipe

---

### B4 — Update recipe endpoint

**Slice:** `Chairly.Api/Features/Clients/UpdateRecipe/`

**Route:** `PUT /api/recipes/{id}`

**Request body:** same shape as Create (without `bookingId` — recipe is identified by `id`).

**Handler logic:**

1. Load recipe by `id` scoped to `TenantId`. Return `404` if not found.
2. Authorisation check: same as B2 (StaffMember can only edit own).
3. Replace `Title`, `Notes`, and `Products` list entirely (full replace, no partial patch).
4. Set `UpdatedAtUtc = UtcNow`, `UpdatedBy = currentUserId`.
5. Return `200 OK` with updated recipe.

**Tests:**
- Returns 200 with updated recipe on happy path
- Returns 404 when recipe not found
- Returns 403 when StaffMember tries to edit another staff member's recipe

---

### B5 — Get client recipe history endpoint

**Slice:** `Chairly.Api/Features/Clients/GetClientRecipes/`

**Route:** `GET /api/clients/{clientId}/recipes`

**Handler logic:**

1. Verify client exists and belongs to `TenantId`. Return `404` if not.
2. Authorisation check:
   - If caller's role is `StaffMember`, filter to only recipes where `recipe.StaffMemberId == currentUserId`.
   - Owner/Manager: return all recipes for the client.
3. Return list ordered by `CreatedAtUtc` descending (most recent first).

**Response body:**

```json
[
  {
    "id": "guid",
    "bookingId": "guid",
    "bookingDate": "datetime",   // StartTime from the booking (joined)
    "staffMemberId": "guid",
    "staffMemberName": "string", // Full name from StaffMember (joined)
    "title": "string",
    "notes": "string?",
    "products": [ ... ],
    "createdAtUtc": "datetime",
    "updatedAtUtc": "datetime?"
  }
]
```

**Tests:**
- Returns empty list when client has no recipes
- Returns list ordered newest-first
- StaffMember only sees their own recipes for the client
- Returns 404 when client not found

---

## Frontend Tasks

### F1 — Recipe models and API service

**Location:** `libs/chairly/src/lib/clients/`

**Models** (`models/recipe.model.ts`):

```typescript
export interface RecipeProduct {
  id?: string;
  name: string;
  brand?: string;
  quantity?: string;
  sortOrder: number;
}

export interface Recipe {
  id: string;
  bookingId: string;
  clientId: string;
  staffMemberId: string;
  title: string;
  notes?: string;
  products: RecipeProduct[];
  createdAtUtc: string;
  createdBy: string;
  updatedAtUtc?: string;
  updatedBy?: string;
}

export interface ClientRecipeSummary {
  id: string;
  bookingId: string;
  bookingDate: string;
  staffMemberId: string;
  staffMemberName: string;
  title: string;
  notes?: string;
  products: RecipeProduct[];
  createdAtUtc: string;
  updatedAtUtc?: string;
}

export interface CreateRecipeRequest {
  bookingId: string;
  title: string;
  notes?: string;
  products: Omit<RecipeProduct, 'id'>[];
}

export interface UpdateRecipeRequest {
  title: string;
  notes?: string;
  products: Omit<RecipeProduct, 'id'>[];
}
```

**API service** (`data-access/recipes.service.ts`):

```typescript
// Methods:
getRecipeByBooking(bookingId: string): Observable<Recipe>
getClientRecipes(clientId: string): Observable<ClientRecipeSummary[]>
createRecipe(request: CreateRecipeRequest): Observable<Recipe>
updateRecipe(id: string, request: UpdateRecipeRequest): Observable<Recipe>
```

---

### F2 — Recipe form dialog (add/edit recipe on a booking)

**Location:** `libs/chairly/src/lib/clients/feature/recipe-form/`

A dialog that opens when a staff member clicks "Recept toevoegen" or "Recept bewerken" on a completed booking detail view.

**Component:** `RecipeFormComponent` (smart component — loads/saves via service directly, no store needed for this dialog)

**Template (`recipe-form.component.html`):**
- Use the native `<dialog>` pattern from CLAUDE.md
- Title field: text input labeled "Titel behandeling"
- Notes field: textarea labeled "Notities"
- Products section: dynamic list with add/remove buttons
  - Each product row: Name (required), Brand, Quantity, with a "Verwijderen" icon button
  - "Product toevoegen" button adds a new empty row
  - Products are reorderable (drag handles, or simple up/down buttons are acceptable)
- "Opslaan" primary button / "Annuleren" secondary button
- Loading state on save button
- Error message display for API errors

**Inputs:**
- `bookingId: InputSignal<string>` — required
- `existingRecipe: InputSignal<Recipe | null>` — if set, form is in edit mode

**Outputs:**
- `saved: OutputEmitterRef<Recipe>` — emits the saved recipe on success
- `cancelled: OutputEmitterRef<void>`

**Behaviour:**
- On open: if `existingRecipe` provided, pre-fill all fields
- On "Opslaan": call `createRecipe` or `updateRecipe` based on mode; emit `saved` on success
- Form validation: title required, each product name required

---

### F3 — Client recipe history panel (within client detail page)

**Location:** `libs/chairly/src/lib/clients/feature/client-detail/` (extend existing, or add alongside)

A section on the client detail page showing the treatment history as a vertical timeline.

**Component:** `ClientRecipeHistoryComponent` (presentational, receives data via input)

**Template (`client-recipe-history.component.html`):**
- Section heading: "Behandelgeschiedenis"
- Empty state: "Nog geen behandelrecords voor deze klant"
- List ordered newest first, each card showing:
  - Date of treatment (formatted as Dutch date: `d MMMM yyyy`)
  - Staff member name
  - Recipe title
  - Notes (collapsed by default, expand on click)
  - Product list (bulleted: "Name — Brand, Quantity")
  - "Bewerken" button (only shown if current user is allowed to edit)
- Clicking "Bewerken" opens `RecipeFormComponent` in edit mode

**Smart wrapper:** extend the existing client detail smart component to:
1. Load client recipes via `RecipesService.getClientRecipes(clientId)`
2. Pass `clientRecipes` signal down to `ClientRecipeHistoryComponent`
3. Handle add-recipe flow: show "Recept toevoegen" button on completed bookings in the booking list section (per booking); open `RecipeFormComponent` dialog

**Route:** No new route needed — this is an extension of the existing client detail page.

**Dutch UI copy:**
- "Behandelgeschiedenis" — section heading
- "Nog geen behandelrecords voor deze klant" — empty state
- "Recept toevoegen" — add button on a completed booking
- "Recept bewerken" — edit button on an existing recipe
- "Titel behandeling" — title field label
- "Notities" — notes field label
- "Producten" — products section heading
- "Product toevoegen" — add product button
- "Naam" — product name
- "Merk" — brand
- "Hoeveelheid" — quantity
- "Verwijderen" — remove button

**Playwright e2e (`apps/chairly-e2e/src/recipes.spec.ts`):**
- Navigate to a client with a completed booking
- Click "Recept toevoegen" on the booking
- Fill in title, notes, and one product; click "Opslaan"
- Verify the new recipe appears in the "Behandelgeschiedenis" section
- Click "Recept bewerken", change the title, save
- Verify updated title appears
- Verify empty state when no recipes exist

---

## Acceptance Criteria

- [ ] `Recipe` entity and `RecipeProduct` owned entity exist in Chairly.Domain
- [ ] EF configuration with unique index on `(TenantId, BookingId)` and history index on `(TenantId, ClientId)`
- [ ] `POST /api/recipes` creates a recipe for a completed booking (201)
- [ ] `POST /api/recipes` returns 422 when booking is not yet completed
- [ ] `POST /api/recipes` returns 409 when a recipe already exists for the booking
- [ ] `GET /api/recipes/booking/{bookingId}` returns the recipe or 404
- [ ] `PUT /api/recipes/{id}` updates an existing recipe
- [ ] `GET /api/clients/{clientId}/recipes` returns history list, newest first
- [ ] StaffMember can only create/view/edit recipes for their own bookings
- [ ] Recipe form dialog opens on a completed booking, allows adding title, notes, and products
- [ ] Client detail page shows "Behandelgeschiedenis" timeline
- [ ] All backend unit and integration tests pass
- [ ] All frontend lint, test, and build checks pass
- [ ] Playwright e2e tests pass
- [ ] All backend quality checks pass (dotnet build, test, format)
- [ ] All frontend quality checks pass (lint, test, build)

---

## Out of Scope

- Deleting recipes (history should never be erased)
- Attaching photos to recipes
- Recipe templates / pre-defined product lists
- Sharing recipes between tenants
- Recipe visibility for clients (client portal not yet implemented)
- Reporting / analytics on products used across clients
