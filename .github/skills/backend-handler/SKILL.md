---
name: backend-handler
description: >
  Handler implementation patterns for Chairly backend using custom mediator and OneOf.
  Use when implementing command/query handlers in VSA slices.
---

# Handler Patterns

## Command (write operation)

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
}
#pragma warning restore CA1812
```

## Query (read operation)

```csharp
using Chairly.Api.Shared.Mediator;
using OneOf;
using OneOf.Types;

namespace Chairly.Api.Features.{Context}.{UseCase};

internal sealed record {UseCase}Query(Guid Id) : IRequest<OneOf<{Response}, NotFound>>;
```

## Handler — simple return (no failure case)

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
            CreatedAtUtc = DateTimeOffset.UtcNow,
#pragma warning disable MA0026
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

## Handler — OneOf return (failure cases)

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

        // apply changes
        entity.UpdatedAtUtc = DateTimeOffset.UtcNow;
#pragma warning disable MA0026
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

## Pragma reference

| Pragma | When |
|---|---|
| `#pragma warning disable CA1812` | Every `internal sealed class` instantiated via DI |
| `#pragma warning disable MA0026` | Every `CreatedBy = Guid.Empty` / `UpdatedBy = Guid.Empty` |
| `.ConfigureAwait(false)` | Every `await` expression |
