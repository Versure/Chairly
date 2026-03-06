---
name: chairly-backend-slice
description: >
  Chairly backend patterns. Use when implementing VSA slices, handlers,
  EF Core entities, configurations, or tests in the Chairly backend.
user-invocable: false
---

# Chairly Backend Patterns

Reference boilerplate for implementing backend features. All patterns are derived from
existing code in `src/backend/`. Always read an existing slice before implementing a
new one to confirm nothing has changed.

---

## Domain Entity

Location: `Chairly.Domain/Entities/{Entity}.cs`

```csharp
namespace Chairly.Domain.Entities;

public class {Entity}
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    // domain properties here

    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid? UpdatedBy { get; set; }
}
```

Rules:
- No EF Core dependency in Domain
- Navigation properties are allowed (e.g. `public ServiceCategory? Category { get; set; }`)
- Status is never stored — derive it from timestamp pairs (`CancelledAtUtc` means cancelled)

---

## EF Core Configuration

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

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(x => x.CreatedBy).IsRequired();
        builder.Property(x => x.UpdatedBy).IsRequired(false);

        // Add indexes and relationships here, e.g.:
        // builder.HasIndex(x => new { x.Name, x.TenantId }).IsUnique();
        // builder.HasOne(x => x.Parent).WithMany().HasForeignKey(x => x.ParentId)
        //     .IsRequired(false).OnDelete(DeleteBehavior.SetNull);
    }
}
#pragma warning restore CA1812
```

After adding the configuration, register it in `ChairlyDbContext` with a new `DbSet<{Entity}>` property.
Then add a migration (see Migration section below).

---

## VSA Slice Structure

All files for one use case: `Chairly.Api/Features/{Context}/{UseCase}/`

### Command (write)

```csharp
using System.ComponentModel.DataAnnotations;
using Chairly.Api.Shared.Mediator;

#pragma warning disable CA1812
namespace Chairly.Api.Features.{Context}.{UseCase};

internal sealed class {UseCase}Command : IRequest<{Response}>
{
    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    // Add other properties with Data Annotations for validation
    // [Range(0, double.MaxValue)] for decimals
    // [MaxLength(2000)] for optional strings
}
#pragma warning restore CA1812
```

### Query (read)

```csharp
using Chairly.Api.Shared.Mediator;
using OneOf;
using OneOf.Types;

namespace Chairly.Api.Features.{Context}.{UseCase};

internal sealed record {UseCase}Query(Guid Id) : IRequest<OneOf<{Response}, NotFound>>;
```

### Handler — simple return (no failure case, e.g. Create)

```csharp
using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Tenancy;
using Chairly.Domain.Entities;
using Chairly.Infrastructure.Persistence;

#pragma warning disable CA1812
namespace Chairly.Api.Features.{Context}.{UseCase};

internal sealed class {UseCase}Handler(ChairlyDbContext db) : IRequestHandler<{UseCase}Command, {Response}>
{
    public async Task<{Response}> Handle({UseCase}Command command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var entity = new {Entity}
        {
            Id = Guid.NewGuid(),
            TenantId = TenantConstants.DefaultTenantId,
            Name = command.Name,
            // map other command properties
            CreatedAtUtc = DateTimeOffset.UtcNow,
#pragma warning disable MA0026 // TODO: Replace with authenticated user ID from Keycloak (see Keycloak integration)
            CreatedBy = Guid.Empty,
#pragma warning restore MA0026
        };

        db.{Entities}.Add(entity);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return ToResponse(entity);
    }

    private static {Response} ToResponse({Entity} entity) =>
        new(entity.Id, entity.Name, entity.CreatedAtUtc, entity.CreatedBy, entity.UpdatedAtUtc, entity.UpdatedBy);
}
#pragma warning restore CA1812
```

### Handler — OneOf return (failure cases, e.g. Update/Delete/Get)

```csharp
using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Tenancy;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;

#pragma warning disable CA1812
namespace Chairly.Api.Features.{Context}.{UseCase};

internal sealed class {UseCase}Handler(ChairlyDbContext db) : IRequestHandler<{UseCase}Command, OneOf<{Response}, NotFound>>
{
    public async Task<OneOf<{Response}, NotFound>> Handle({UseCase}Command command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var entity = await db.{Entities}
            .FirstOrDefaultAsync(x => x.Id == command.Id && x.TenantId == TenantConstants.DefaultTenantId, cancellationToken)
            .ConfigureAwait(false);

        if (entity is null)
        {
            return new NotFound();
        }

        entity.Name = command.Name;
        entity.UpdatedAtUtc = DateTimeOffset.UtcNow;
#pragma warning disable MA0026 // TODO: Replace with authenticated user ID from Keycloak (see Keycloak integration)
        entity.UpdatedBy = Guid.Empty;
#pragma warning restore MA0026

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return ToResponse(entity);
    }

    private static {Response} ToResponse({Entity} entity) =>
        new(entity.Id, entity.Name, entity.CreatedAtUtc, entity.CreatedBy, entity.UpdatedAtUtc, entity.UpdatedBy);
}
#pragma warning restore CA1812
```

### Endpoint — POST (returns 201 Created)

```csharp
using Chairly.Api.Shared.Mediator;

namespace Chairly.Api.Features.{Context}.{UseCase};

internal static class {UseCase}Endpoint
{
    public static void Map{UseCase}(this RouteGroupBuilder group)
    {
        group.MapPost("/", async (
            {UseCase}Command command,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(command, cancellationToken).ConfigureAwait(false);
            return Results.Created($"/api/{context}/{result.Id}", result);
        });
    }
}
```

### Endpoint — PUT/PATCH with OneOf (200 OK or 404)

```csharp
group.MapPut("/{id:guid}", async (
    Guid id,
    {UseCase}Command command,
    IMediator mediator,
    CancellationToken cancellationToken) =>
{
    command.Id = id;
    var result = await mediator.Send(command, cancellationToken).ConfigureAwait(false);
    return result.Match(
        entity => Results.Ok(entity),
        _ => Results.NotFound());
});
```

### Endpoint — DELETE with OneOf (204 No Content or 404)

```csharp
group.MapDelete("/{id:guid}", async (
    Guid id,
    IMediator mediator,
    CancellationToken cancellationToken) =>
{
    var result = await mediator.Send(new {UseCase}Command(id), cancellationToken).ConfigureAwait(false);
    return result.Match(
        _ => Results.NoContent(),
        _ => Results.NotFound());
});
```

### Endpoint group file

Location: `Chairly.Api/Features/{Context}/{Context}Endpoints.cs`

```csharp
namespace Chairly.Api.Features.{Context};

internal static class {Context}Endpoints
{
    public static IEndpointRouteBuilder Map{Context}Endpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/{context}");

        group.Map{UseCase1}();
        group.Map{UseCase2}();

        return app;
    }
}
```

Register in `Program.cs`: `app.Map{Context}Endpoints();`

---

## Response Record

Location: `Chairly.Api/Features/{Context}/{Entity}Response.cs` (shared across slices in same context)

```csharp
namespace Chairly.Api.Features.{Context};

internal sealed record {Entity}Response(
    Guid Id,
    string Name,
    // other properties
    DateTimeOffset CreatedAtUtc,
    Guid CreatedBy,
    DateTimeOffset? UpdatedAtUtc,
    Guid? UpdatedBy);
```

---

## EF Core Migration

Run from INSIDE the worktree (`.worktrees/backend/`):

```bash
cd .worktrees/backend && dotnet ef migrations add {MigrationName} \
  --project src/backend/Chairly.Infrastructure \
  --startup-project src/backend/Chairly.Api
```

Paths are relative to the worktree root. Migration files appear in
`Chairly.Infrastructure/Migrations/`.

---

## Unit Tests

Location: `Chairly.Tests/Features/{Context}/{Entity}HandlerTests.cs`

```csharp
using System.ComponentModel.DataAnnotations;
using Chairly.Api.Features.{Context}.Create{Entity};
using Chairly.Api.Features.{Context}.Update{Entity};
using Chairly.Api.Shared.Tenancy;
using Chairly.Domain.Entities;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using OneOf.Types;

namespace Chairly.Tests.Features.{Context};

public class {Entity}HandlerTests
{
    private static ChairlyDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ChairlyDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ChairlyDbContext(options);
    }

    private static {Entity} CreateTest{Entity}(ChairlyDbContext db)
    {
        var entity = new {Entity}
        {
            Id = Guid.NewGuid(),
            TenantId = TenantConstants.DefaultTenantId,
            Name = "Test {Entity}",
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
        db.{Entities}.Add(entity);
        db.SaveChanges();
        return entity;
    }

    [Fact]
    public async Task Create{Entity}Handler_HappyPath_Creates{Entity}()
    {
        await using var db = CreateDbContext();
        var handler = new Create{Entity}Handler(db);
        var command = new Create{Entity}Command { Name = "Test Name" };

        var result = await handler.Handle(command);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("Test Name", result.Name);
        Assert.Equal(TenantConstants.DefaultTenantId, result.TenantId ?? Guid.Empty);
        Assert.Equal(1, await db.{Entities}.CountAsync());
    }

    [Fact]
    public void Create{Entity}Command_EmptyName_FailsValidation()
    {
        var command = new Create{Entity}Command { Name = string.Empty };
        var results = new List<ValidationResult>();
        var context = new ValidationContext(command);

        var isValid = Validator.TryValidateObject(command, context, results, validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(Create{Entity}Command.Name), StringComparer.Ordinal));
    }

    [Fact]
    public async Task Update{Entity}Handler_HappyPath_SetsUpdatedAtUtc()
    {
        await using var db = CreateDbContext();
        var existing = CreateTest{Entity}(db);
        var handler = new Update{Entity}Handler(db);
        var command = new Update{Entity}Command { Id = existing.Id, Name = "Updated" };

        var result = await handler.Handle(command);

        var response = result.AsT0;
        Assert.Equal("Updated", response.Name);
        Assert.NotNull(response.UpdatedAtUtc);
    }

    [Fact]
    public async Task Update{Entity}Handler_NotFound_ReturnsNotFound()
    {
        await using var db = CreateDbContext();
        var handler = new Update{Entity}Handler(db);

        var result = await handler.Handle(new Update{Entity}Command { Id = Guid.NewGuid(), Name = "Any" });

        Assert.True(result.IsT1);
        Assert.IsType<NotFound>(result.AsT1);
    }
}
```

---

## Pragma Reference

| Pragma | When to use |
|---|---|
| `#pragma warning disable CA1812` / `restore` | Wrap every `internal sealed class` that is instantiated via DI or reflection (handlers, configurations, validators) |
| `#pragma warning disable MA0026` / `restore` | Wrap every `CreatedBy = Guid.Empty` and `UpdatedBy = Guid.Empty` assignment |
| `.ConfigureAwait(false)` | Append to EVERY `await` expression without exception |
