---
name: backend-ef-config
description: >
  EF Core entity configuration and migration patterns for Chairly backend.
  Use when creating database configurations, DbSet registrations, or migrations.
---

# EF Core Configuration Pattern

Location: `Chairly.Infrastructure/Persistence/Configurations/{Entity}Configuration.cs`

```csharp
using Chairly.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

#pragma warning disable CA1812 // Instantiated via ApplyConfigurationsFromAssembly
namespace Chairly.Infrastructure.Persistence.Configurations;

internal sealed class {Entity}Configuration : IEntityTypeConfiguration<{Entity}>
{
    public void Configure(EntityTypeBuilder<{Entity}> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.Name).IsRequired().HasMaxLength(150);
        builder.Property(x => x.CreatedBy).IsRequired();
        builder.Property(x => x.UpdatedBy).IsRequired(false);

        // Indexes and relationships:
        // builder.HasIndex(x => new { x.Name, x.TenantId }).IsUnique();
        // builder.HasOne(x => x.Parent).WithMany().HasForeignKey(x => x.ParentId)
        //     .IsRequired(false).OnDelete(DeleteBehavior.SetNull);
    }
}
#pragma warning restore CA1812
```

After adding the configuration, register a `DbSet<{Entity}>` property in `ChairlyDbContext`.

## Migration

Run from inside the worktree or project root:

```bash
dotnet ef migrations add {MigrationName} \
  --project src/backend/Chairly.Infrastructure \
  --startup-project src/backend/Chairly.Api
```

## Idempotent migration rules

All migrations MUST be idempotent:
- `CreateTable` → use raw SQL: `CREATE TABLE IF NOT EXISTS`
- `CreateIndex` → use: `CREATE INDEX IF NOT EXISTS`
- `AddColumn` → wrap in `DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'T' AND column_name = 'C') THEN ALTER TABLE "T" ADD COLUMN "C" ...; END IF; END $$;`

Never use bare `migrationBuilder.CreateTable()`, `migrationBuilder.CreateIndex()`, or `migrationBuilder.AddColumn()`.
