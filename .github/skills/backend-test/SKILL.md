---
name: backend-test
description: >
  xUnit test patterns for Chairly backend handlers.
  Use when writing unit tests for command/query handlers.
---

# Backend Test Patterns

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

## Test naming convention

`{MethodName}_{Scenario}_{ExpectedResult}`

## What to test

- Happy path for every handler
- Validation failures (Data Annotations)
- Not-found cases (OneOf returns `NotFound`)
- Edge cases specific to business logic
