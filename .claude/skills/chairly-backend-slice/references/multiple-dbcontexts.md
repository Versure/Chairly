# Multiple DbContexts

When adding a second `DbContext` (e.g. `WebsiteDbContext` alongside `ChairlyDbContext`), EF Core
configuration classes from the same assembly must be explicitly filtered.

**Problem:** `ApplyConfigurationsFromAssembly()` discovers ALL `IEntityTypeConfiguration<T>`
implementations in the assembly, regardless of which `DbContext` they belong to. This causes
`PendingModelChangesWarning` on the wrong context.

**Solution:** Filter configurations by namespace:

```csharp
// In ChairlyDbContext — exclude Website configurations:
modelBuilder.ApplyConfigurationsFromAssembly(
    typeof(ChairlyDbContext).Assembly,
    t => t.Namespace != null && !t.Namespace.Contains("Website", StringComparison.Ordinal));

// In WebsiteDbContext — include ONLY Website configurations:
modelBuilder.ApplyConfigurationsFromAssembly(
    typeof(WebsiteDbContext).Assembly,
    t => t.Namespace != null && t.Namespace.Contains("Website", StringComparison.Ordinal));
```

Place Website-specific configurations in a `Configurations/Website/` subfolder so the namespace
filter works reliably.
