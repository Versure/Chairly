# Idempotent Migrations

> **Status: Implemented** — Merged to main.

## Overview

EF Core migrations crash on startup when a database table already exists but is not recorded in `__EFMigrationsHistory`. This happens during local development when a migration was applied outside EF Core (e.g., a previous `dotnet ef database update` that was partially undone, or the database was created manually). The fix makes all existing migrations idempotent by replacing `CreateTable` and `CreateIndex` calls with raw SQL using `IF NOT EXISTS` guards, wraps `AddColumn` calls with column-existence checks, and documents the convention so future migrations follow the same pattern. Fixes GitHub issue #35.

## Domain Context

- Bounded context: Infrastructure (not a product domain)
- Key entities involved: all existing entities (migrations are idempotent wrappers for ServiceCategories, Services, StaffMembers, Clients, Bookings, BookingServices, Invoices, InvoiceLineItems, Recipes, RecipeProducts)
- Key files:
  - `src/backend/Chairly.Infrastructure/Migrations/` — all existing migration files
  - `CLAUDE.md` — repo-wide conventions for writing future idempotent migrations
  - `.claude/skills/feature-team/phase-1-backend.md` — agent instructions for new migrations

---

## Backend Tasks

### B1 — Make all existing CreateTable migrations idempotent

For each migration file in `src/backend/Chairly.Infrastructure/Migrations/` that contains EF Core API calls for `CreateTable`, `CreateIndex`, or `AddColumn`, replace those calls with raw `migrationBuilder.Sql(...)` equivalents using `IF NOT EXISTS` guards.

**Do NOT modify `*.Designer.cs` files** — they are EF Core metadata snapshots and must remain untouched.

**Do NOT modify `Down()` methods** — rollback operations should remain as-is.

---

#### Pattern: CreateTable → CREATE TABLE IF NOT EXISTS

Replace:
```csharp
migrationBuilder.CreateTable(
    name: "TableName",
    columns: table => new { ... },
    constraints: table => { ... });
```

With:
```csharp
migrationBuilder.Sql("""
    CREATE TABLE IF NOT EXISTS "TableName" (
        "Col1" uuid NOT NULL,
        "Col2" text NOT NULL,
        ...
        CONSTRAINT "PK_TableName" PRIMARY KEY ("Id")
    );
    """);
```

For tables with foreign keys, include the `CONSTRAINT` clause inside the `CREATE TABLE` body:
```csharp
migrationBuilder.Sql("""
    CREATE TABLE IF NOT EXISTS "ChildTable" (
        "Id" uuid NOT NULL,
        "ParentId" uuid NOT NULL,
        ...
        CONSTRAINT "PK_ChildTable" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_ChildTable_ParentTable_ParentId" FOREIGN KEY ("ParentId")
            REFERENCES "ParentTable" ("Id") ON DELETE CASCADE
    );
    """);
```

**PostgreSQL type mappings to use in raw SQL:**

| EF Core C# type | EF `type:` annotation | Raw SQL type |
|---|---|---|
| `Guid` | `uuid` | `uuid` |
| `string` (with maxLength) | `character varying(N)` | `character varying(N)` |
| `string` (no maxLength) | `text` | `text` |
| `bool` | `boolean` | `boolean` |
| `int` | `integer` | `integer` |
| `decimal` (precision 10,2) | `numeric(10,2)` | `numeric(10,2)` |
| `decimal` (precision 18,2) | `numeric(18,2)` | `numeric(18,2)` |
| `decimal` (precision 5,2) | `numeric(5,2)` | `numeric(5,2)` |
| `DateTimeOffset` | `timestamp with time zone` | `timestamp with time zone` |
| `DateOnly` | `date` | `date` |
| `TimeSpan` | `interval` | `interval` |

Nullable columns get a bare type (no `NOT NULL`). Non-nullable columns get `NOT NULL`.

---

#### Pattern: CreateIndex → CREATE INDEX IF NOT EXISTS

Replace:
```csharp
migrationBuilder.CreateIndex(
    name: "IX_TableName_Col",
    table: "TableName",
    column: "Col");
```

With:
```csharp
migrationBuilder.Sql("""
    CREATE INDEX IF NOT EXISTS "IX_TableName_Col" ON "TableName" ("Col");
    """);
```

For composite indexes:
```csharp
migrationBuilder.Sql("""
    CREATE INDEX IF NOT EXISTS "IX_TableName_Col1_Col2" ON "TableName" ("Col1", "Col2");
    """);
```

For unique indexes (where the original `CreateIndex` had `unique: true`):
```csharp
migrationBuilder.Sql("""
    CREATE UNIQUE INDEX IF NOT EXISTS "IX_TableName_Col" ON "TableName" ("Col");
    """);
```

For descending indexes (where the original `CreateIndex` had `descending: new[] { false, true }`):
```csharp
migrationBuilder.Sql("""
    CREATE INDEX IF NOT EXISTS "IX_TableName_Col1_Col2" ON "TableName" ("Col1", "Col2" DESC);
    """);
```

---

#### Pattern: AddColumn → DO $$ IF NOT EXISTS THEN ALTER TABLE

Replace:
```csharp
migrationBuilder.AddColumn<T>(
    name: "ColumnName",
    table: "TableName",
    type: "sql_type",
    nullable: false,
    defaultValue: ...);
```

With:
```csharp
migrationBuilder.Sql("""
    DO $$
    BEGIN
        IF NOT EXISTS (
            SELECT 1 FROM information_schema.columns
            WHERE table_name = 'TableName' AND column_name = 'ColumnName'
        ) THEN
            ALTER TABLE "TableName" ADD COLUMN "ColumnName" sql_type NOT NULL DEFAULT default_value;
        END IF;
    END $$;
    """);
```

For nullable columns (no default needed):
```csharp
migrationBuilder.Sql("""
    DO $$
    BEGIN
        IF NOT EXISTS (
            SELECT 1 FROM information_schema.columns
            WHERE table_name = 'TableName' AND column_name = 'ColumnName'
        ) THEN
            ALTER TABLE "TableName" ADD COLUMN "ColumnName" sql_type;
        END IF;
    END $$;
    """);
```

**Note on defaults in `AddColumn`:** The EF Core `defaultValue` is used for backfilling existing rows on first apply. In the idempotent version, include the `DEFAULT` clause only when the original migration had a non-zero/non-empty default. After the `DO $$` block, EF Core will still record the migration in `__EFMigrationsHistory` and the migration won't run again, so the DEFAULT clause is only for the very first apply (where the column doesn't yet exist).

---

#### AlterColumn

`migrationBuilder.AlterColumn(...)` calls that change a column type or collation are intrinsically non-idempotent. They are present in `20260305152328_ConvertStaffRoleToString.cs` as a raw SQL call already:
```csharp
migrationBuilder.Sql("ALTER TABLE \"StaffMembers\" ALTER COLUMN \"Role\" TYPE text USING CASE ...");
```

This migration already uses raw SQL (not the EF API), so it requires no change. Leave it as-is.

---

#### Files to update (in order)

**1. `20260226081546_InitialCreate.cs`** — creates `ServiceCategories` and `Services` tables plus two indexes

Operations to convert:
- `CreateTable("ServiceCategories", ...)` → raw SQL `CREATE TABLE IF NOT EXISTS "ServiceCategories" (...)`
- `CreateTable("Services", ...)` with FK to ServiceCategories → raw SQL `CREATE TABLE IF NOT EXISTS "Services" (... CONSTRAINT "FK_..." FOREIGN KEY ...)`
- `CreateIndex("IX_Services_CategoryId", ...)` → `CREATE INDEX IF NOT EXISTS ...`
- `CreateIndex("IX_Services_Name_TenantId", ..., unique: true)` → `CREATE UNIQUE INDEX IF NOT EXISTS ...`

**2. `20260227065057_AddAuditFields.cs`** — adds `CreatedBy`/`UpdatedBy` to `Services` and `CreatedAtUtc`/`CreatedBy` to `ServiceCategories`

Operations to convert:
- `AddColumn<Guid>("CreatedBy", "Services", ...)` → `DO $$ IF NOT EXISTS ... ALTER TABLE "Services" ADD COLUMN "CreatedBy" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000' $$`
- `AddColumn<Guid>("UpdatedBy", "Services", nullable: true)` → `DO $$ IF NOT EXISTS ... ALTER TABLE "Services" ADD COLUMN "UpdatedBy" uuid $$`
- `AddColumn<DateTimeOffset>("CreatedAtUtc", "ServiceCategories", ...)` → `DO $$ IF NOT EXISTS ... ALTER TABLE "ServiceCategories" ADD COLUMN "CreatedAtUtc" timestamp with time zone NOT NULL DEFAULT '0001-01-01 00:00:00+00' $$`
- `AddColumn<Guid>("CreatedBy", "ServiceCategories", ...)` → `DO $$ IF NOT EXISTS ... ALTER TABLE "ServiceCategories" ADD COLUMN "CreatedBy" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000' $$`

**3. `20260305143723_AddStaffMember.cs`** — creates `StaffMembers` table and one index

Operations to convert:
- `CreateTable("StaffMembers", ...)` → raw SQL `CREATE TABLE IF NOT EXISTS "StaffMembers" (...)`
- `CreateIndex("IX_StaffMembers_FirstName_LastName_TenantId", ...)` → `CREATE INDEX IF NOT EXISTS ...`

**4. `20260305152328_ConvertStaffRoleToString.cs`** — already uses `migrationBuilder.Sql(...)`. No changes needed.

**5. `20260306071435_AddClient.cs`** — creates `Clients` table and one index

Operations to convert:
- `CreateTable("Clients", ...)` → raw SQL `CREATE TABLE IF NOT EXISTS "Clients" (...)`
- `CreateIndex("IX_Clients_TenantId", ...)` → `CREATE INDEX IF NOT EXISTS ...`

**6. `20260306163350_AddBookingEntities.cs`** — creates `Bookings` and `BookingServices` tables plus five indexes

Operations to convert:
- `CreateTable("Bookings", ...)` with two FKs (to Clients and StaffMembers) → raw SQL with FK constraints
- `CreateTable("BookingServices", ...)` with one FK (to Bookings, cascade delete) → raw SQL with FK constraint
- `CreateIndex("IX_Bookings_ClientId", ...)` → `CREATE INDEX IF NOT EXISTS ...`
- `CreateIndex("IX_Bookings_StaffMemberId", ...)` → `CREATE INDEX IF NOT EXISTS ...`
- `CreateIndex("IX_Bookings_TenantId_StaffMemberId_StartTime", ...)` → `CREATE INDEX IF NOT EXISTS ...`
- `CreateIndex("IX_Bookings_TenantId_StartTime", ...)` → `CREATE INDEX IF NOT EXISTS ...`
- `CreateIndex("IX_BookingServices_BookingId", ...)` → `CREATE INDEX IF NOT EXISTS ...`

**7. `20260308121309_AddBookingTenantIdIndex.cs`** — adds one index on `Bookings`

Operations to convert:
- `CreateIndex("IX_Bookings_TenantId", ...)` → `CREATE INDEX IF NOT EXISTS ...`

**8. `20260310092852_AddInvoices.cs`** — creates `Invoices` and `InvoiceLineItems` tables plus five indexes

Operations to convert:
- `CreateTable("Invoices", ...)` → raw SQL `CREATE TABLE IF NOT EXISTS "Invoices" (...)`
- `CreateTable("InvoiceLineItems", ...)` with FK to Invoices (cascade delete) → raw SQL with FK constraint
- `CreateIndex("IX_InvoiceLineItems_InvoiceId", ...)` → `CREATE INDEX IF NOT EXISTS ...`
- `CreateIndex("IX_Invoices_TenantId_BookingId", ..., unique: true)` → `CREATE UNIQUE INDEX IF NOT EXISTS ...`
- `CreateIndex("IX_Invoices_TenantId_ClientId", ...)` → `CREATE INDEX IF NOT EXISTS ...`
- `CreateIndex("IX_Invoices_TenantId_CreatedAtUtc", ..., descending: new[] { false, true })` → `CREATE INDEX IF NOT EXISTS ... ON "Invoices" ("TenantId", "CreatedAtUtc" DESC)`
- `CreateIndex("IX_Invoices_TenantId_InvoiceNumber", ..., unique: true)` → `CREATE UNIQUE INDEX IF NOT EXISTS ...`

**9. `20260310101845_AddRecipes.cs`** — already fully idempotent (uses raw SQL throughout). No changes needed.

**10. `20260310140759_AddInvoiceVatAndManualLineItems.cs`** — adds five columns to `Invoices` and `InvoiceLineItems`

Operations to convert:
- `AddColumn<decimal>("SubTotalAmount", "Invoices", ...)` → `DO $$ IF NOT EXISTS ... ADD COLUMN "SubTotalAmount" numeric(18,2) NOT NULL DEFAULT 0 $$`
- `AddColumn<decimal>("TotalVatAmount", "Invoices", ...)` → `DO $$ IF NOT EXISTS ... ADD COLUMN "TotalVatAmount" numeric(18,2) NOT NULL DEFAULT 0 $$`
- `AddColumn<bool>("IsManual", "InvoiceLineItems", ...)` → `DO $$ IF NOT EXISTS ... ADD COLUMN "IsManual" boolean NOT NULL DEFAULT false $$`
- `AddColumn<decimal>("VatAmount", "InvoiceLineItems", ...)` → `DO $$ IF NOT EXISTS ... ADD COLUMN "VatAmount" numeric(18,2) NOT NULL DEFAULT 0 $$`
- `AddColumn<decimal>("VatPercentage", "InvoiceLineItems", ...)` → `DO $$ IF NOT EXISTS ... ADD COLUMN "VatPercentage" numeric(5,2) NOT NULL DEFAULT 0 $$`

---

**After all edits:** run `dotnet build src/backend/Chairly.slnx` to verify no compile errors, then `dotnet test src/backend/Chairly.slnx` to verify all tests pass.

**Test cases to cover:**
- `dotnet build` produces zero errors and zero warnings that weren't there before
- `dotnet test` passes: handler unit tests and integration tests continue to work
  (unit tests use in-memory DbContext and never run migrations; integration tests that apply migrations should be verified)

---

### B2 — Add idempotency convention to CLAUDE.md and phase-1-backend.md

Document the idempotent migration convention in two places so future development agents and human developers always produce idempotent migrations.

**File 1: `CLAUDE.md` (repo root)**

In the `## Code Conventions — Backend` section, under the `**Patterns:**` bullet list, add the following bullet (after the existing timestamp-pair pattern):

```markdown
- **EF Core migrations must be idempotent**: All `CreateTable` calls must use raw SQL with `CREATE TABLE IF NOT EXISTS`. All `CreateIndex` calls must use `CREATE INDEX IF NOT EXISTS`. `AddColumn` calls must use `DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'T' AND column_name = 'C') THEN ALTER TABLE "T" ADD COLUMN "C" ...; END IF; END $$;` blocks. Never use bare `migrationBuilder.CreateTable()`, `migrationBuilder.CreateIndex()`, or `migrationBuilder.AddColumn()` in new migrations.
```

**File 2: `.claude/skills/feature-team/phase-1-backend.md`**

In the `### 2. EF Core configuration (if new entity)` section, after the `dotnet ef migrations add` command block, insert the following instruction block:

```markdown
**CRITICAL: After generating the migration, open the generated `.cs` file (not the `.Designer.cs`) and make it idempotent:**

Replace every `migrationBuilder.CreateTable(...)` with:
```csharp
migrationBuilder.Sql("""
    CREATE TABLE IF NOT EXISTS "TableName" (
        "Id" uuid NOT NULL,
        ...column definitions...
        CONSTRAINT "PK_TableName" PRIMARY KEY ("Id")
    );
    """);
```

Replace every `migrationBuilder.CreateIndex(...)` with:
```csharp
migrationBuilder.Sql("""
    CREATE [UNIQUE] INDEX IF NOT EXISTS "IX_..." ON "TableName" (...);
    """);
```

Replace every `migrationBuilder.AddColumn(...)` with:
```csharp
migrationBuilder.Sql("""
    DO $$
    BEGIN
        IF NOT EXISTS (
            SELECT 1 FROM information_schema.columns
            WHERE table_name = 'TableName' AND column_name = 'ColumnName'
        ) THEN
            ALTER TABLE "TableName" ADD COLUMN "ColumnName" sql_type [NOT NULL DEFAULT value];
        END IF;
    END $$;
    """);
```

**Do NOT edit the `*.Designer.cs` snapshot file** — only the plain `.cs` migration file is modified.
```

**Test cases to cover:**
- Verify `CLAUDE.md` contains the new idempotency bullet under `**Patterns:**`
- Verify `phase-1-backend.md` contains the CRITICAL block after the `dotnet ef migrations add` command
- `dotnet build` still passes after the documentation changes (no C# files changed)

---

## Acceptance Criteria

- [ ] All `CreateTable` calls in existing migrations replaced with `CREATE TABLE IF NOT EXISTS` raw SQL
- [ ] All `CreateIndex` calls in existing migrations replaced with `CREATE INDEX IF NOT EXISTS` raw SQL
- [ ] `AddColumn` calls in existing migrations wrapped with column-existence `DO $$ ... $$` blocks
- [ ] `AlterColumn` and already-raw-SQL migrations left unchanged
- [ ] `*.Designer.cs` files are not modified
- [ ] `Down()` methods are not modified
- [ ] `dotnet build src/backend/Chairly.slnx` passes after changes
- [ ] `dotnet test src/backend/Chairly.slnx` passes after changes
- [ ] `dotnet format src/backend/Chairly.slnx --verify-no-changes` passes after changes
- [ ] `CLAUDE.md` updated with idempotent migration convention under `**Patterns:**`
- [ ] `phase-1-backend.md` updated to instruct the backend agent to make new migrations idempotent
- [ ] Running the app against a database where tables already exist does not crash on startup
- [ ] All backend quality checks pass (dotnet build, test, format)

## Out of Scope

- Frontend changes
- Changing EF Core's `MigrationsSqlGenerator` to automatically generate `IF NOT EXISTS`
- Adding CI steps to verify migration idempotency automatically
- Making `Down()` / rollback methods idempotent (rollback is a manual operation)
- Modifying `*.Designer.cs` snapshot files
