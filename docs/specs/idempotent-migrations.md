# Idempotent Migrations

## Overview

EF Core migrations crash on startup when a database table already exists but is not recorded in `__EFMigrationsHistory`. This happens during local development when a migration was applied outside EF Core (e.g., a previous `dotnet ef database update` that was partially undone, or the database was created manually). The fix makes all existing migrations idempotent by replacing `CreateTable` calls with `IF NOT EXISTS` raw SQL, and documents the convention so future migrations follow the same pattern. Fixes GitHub issue #35.

## Domain Context

- Bounded context: Infrastructure (not a product domain)
- Key entities involved: all existing entities (migrations are idempotent wrappers)
- Key files:
  - `src/backend/Chairly.Infrastructure/Migrations/` — all existing migration files
  - `CLAUDE.md` — conventions for writing future idempotent migrations

---

## Backend Tasks

### B1 — Make all existing CreateTable migrations idempotent

For each migration file in `src/backend/Chairly.Infrastructure/Migrations/` that contains `migrationBuilder.CreateTable(...)`, replace the EF Core API call with a raw `migrationBuilder.Sql(...)` call using `CREATE TABLE IF NOT EXISTS`.

**Pattern to follow for each table:**

Replace:
```csharp
migrationBuilder.CreateTable(
    name: "TableName",
    columns: table => new { ... },
    constraints: table => { ... });
```

With equivalent raw SQL:
```csharp
migrationBuilder.Sql("""
    CREATE TABLE IF NOT EXISTS "TableName" (
        "Id" uuid NOT NULL,
        "TenantId" uuid NOT NULL,
        ...
        CONSTRAINT "PK_TableName" PRIMARY KEY ("Id")
    );
    """);
```

For **indexes** created after `CreateTable`, similarly wrap them:
```csharp
migrationBuilder.Sql("""
    CREATE INDEX IF NOT EXISTS "IX_TableName_TenantId" ON "TableName" ("TenantId");
    """);
```

For **unique indexes**:
```csharp
migrationBuilder.Sql("""
    CREATE UNIQUE INDEX IF NOT EXISTS "IX_TableName_Col" ON "TableName" ("Col");
    """);
```

For **`AddColumn` operations** (in later migrations), wrap with existence checks:
```csharp
migrationBuilder.Sql("""
    DO $$
    BEGIN
        IF NOT EXISTS (
            SELECT 1 FROM information_schema.columns
            WHERE table_name = 'TableName' AND column_name = 'ColumnName'
        ) THEN
            ALTER TABLE "TableName" ADD COLUMN "ColumnName" text;
        END IF;
    END $$;
    """);
```

**Files to update:**
Go through each migration file in order:
1. `20260226081546_InitialCreate.cs` — creates `ServiceCategories` and `Services` tables
2. `20260227065057_AddAuditFields.cs` — adds columns to existing tables
3. `20260305143723_AddStaffMember.cs` — creates `StaffMembers` table
4. `20260305152328_ConvertStaffRoleToString.cs` — alters column
5. `20260306071435_AddClient.cs` — creates `Clients` table
6. Any subsequent migrations for `Bookings`, `Invoices`, `Recipes`, etc.

For each migration, scan the `Up()` method and replace:
- `migrationBuilder.CreateTable(...)` → `migrationBuilder.Sql("CREATE TABLE IF NOT EXISTS ...")`
- `migrationBuilder.CreateIndex(...)` → `migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS ...")`
- `migrationBuilder.AddColumn(...)` → `migrationBuilder.Sql("DO $$ BEGIN IF NOT EXISTS (...) THEN ALTER TABLE ... END IF; END $$;")`
- `migrationBuilder.AlterColumn(...)` → keep as-is OR wrap with `IF EXISTS` check depending on context (altering to add nullable column is usually safe to run again; altering type is not idempotent and requires care)

**Down() methods:** leave unchanged (rollback operations should remain as-is).

**Important:** after replacing migration code, run `dotnet build src/backend/Chairly.slnx` to ensure no compile errors. The migration Designer files (`*.Designer.cs`) must NOT be modified — they are read-only EF Core metadata.

**Tests:** run `dotnet test src/backend/Chairly.slnx` after changes. If any tests rely on in-memory migrations, they continue to work because the migrations are never applied in unit tests (handlers use in-memory DbContext). Integration tests that run real migrations should be checked.

---

### B2 — Add idempotency convention to CLAUDE.md and Phase 1 backend agent

**File 1: `CLAUDE.md` (repo root)**

In the `## Code Conventions — Backend` section, under **Patterns**, add:

```markdown
- **EF Core migrations must be idempotent**: All `CreateTable` calls must use raw SQL with `CREATE TABLE IF NOT EXISTS`. All `CreateIndex` calls must use `CREATE INDEX IF NOT EXISTS`. `AddColumn` calls must use `DO $$ BEGIN IF NOT EXISTS ... THEN ALTER TABLE ... END IF; END $$;` blocks. Never use bare `migrationBuilder.CreateTable()` in new migrations.
```

**File 2: `.claude/skills/feature-team/phase-1-backend.md`**

In the `### 2. EF Core configuration (if new entity)` section, after the `dotnet ef migrations add` command, add:

```markdown
**CRITICAL: After generating the migration, open the generated `.cs` file and make it idempotent:**

Replace every `migrationBuilder.CreateTable(...)` with:
```csharp
migrationBuilder.Sql("""
    CREATE TABLE IF NOT EXISTS "TableName" (
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
            ALTER TABLE "TableName" ADD COLUMN "ColumnName" ...;
        END IF;
    END $$;
    """);
```

**Do NOT edit the `*.Designer.cs` snapshot file.**
```

---

## Acceptance Criteria

- [ ] All `CreateTable` calls in existing migrations replaced with `CREATE TABLE IF NOT EXISTS` raw SQL
- [ ] All `CreateIndex` calls in existing migrations replaced with `CREATE INDEX IF NOT EXISTS` raw SQL
- [ ] `AddColumn` calls in existing migrations wrapped with column-existence checks
- [ ] `dotnet build src/backend/Chairly.slnx` passes after changes
- [ ] `dotnet test src/backend/Chairly.slnx` passes after changes
- [ ] `CLAUDE.md` updated with idempotent migration convention
- [ ] `phase-1-backend.md` updated to instruct the backend agent to make new migrations idempotent
- [ ] Running the app against a database where tables already exist does not crash on startup
- [ ] All backend quality checks pass (dotnet build, test, format)

## Out of Scope

- Frontend changes
- Changing EF Core's `MigrationsSqlGenerator` to automatically generate `IF NOT EXISTS`
- Adding CI steps to verify migration idempotency automatically
- Making `Down()` / rollback methods idempotent (out of scope; rollback is a manual operation)
