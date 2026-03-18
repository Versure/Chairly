---
name: backend-entity
description: >
  Domain entity and value object patterns for Chairly backend.
  Use when creating new entities in Chairly.Domain/Entities/.
---

# Domain Entity Pattern

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

## Rules

- No EF Core dependency in Domain project
- Navigation properties are allowed (e.g. `public ServiceCategory? Category { get; set; }`)
- Status is never stored — derive it from timestamp pairs (`CancelledAtUtc` means cancelled)
- `CreatedAtUtc`/`CreatedBy` required on all entities
- All entities carry `TenantId`

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
