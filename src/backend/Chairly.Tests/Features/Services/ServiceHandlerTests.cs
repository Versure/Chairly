using System.ComponentModel.DataAnnotations;
using Chairly.Api.Features.Services.CreateService;
using Chairly.Api.Features.Services.DeleteService;
using Chairly.Api.Features.Services.ToggleServiceActive;
using Chairly.Api.Features.Services.UpdateService;
using Chairly.Domain.Entities;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using OneOf.Types;

namespace Chairly.Tests.Features.Services;

public class ServiceHandlerTests
{
    private static ChairlyDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ChairlyDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ChairlyDbContext(options);
    }

    private static Service CreateTestService(ChairlyDbContext db, bool isActive = true)
    {
        var service = new Service
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Name = "Test Service",
            Duration = TimeSpan.FromMinutes(30),
            Price = 25.00m,
            IsActive = isActive,
            SortOrder = 0,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
        db.Services.Add(service);
        db.SaveChanges();
        return service;
    }

    [Fact]
    public async Task CreateServiceHandler_HappyPath_CreatesServiceWithIsActiveTrue()
    {
        await using var db = CreateDbContext();
        var handler = new CreateServiceHandler(db);
        var command = new CreateServiceCommand
        {
            Name = "Men's Haircut",
            Duration = TimeSpan.FromMinutes(30),
            Price = 25.00m,
            SortOrder = 1,
        };

        var result = await handler.Handle(command);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("Men's Haircut", result.Name);
        Assert.True(result.IsActive);
        Assert.Equal(1, await db.Services.CountAsync());
    }

    [Fact]
    public void CreateServiceCommand_EmptyName_FailsValidation()
    {
        var command = new CreateServiceCommand
        {
            Name = string.Empty,
            Duration = TimeSpan.FromMinutes(30),
            Price = 25.00m,
        };
        var results = new List<ValidationResult>();
        var context = new ValidationContext(command);

        var isValid = Validator.TryValidateObject(command, context, results, validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateServiceCommand.Name), StringComparer.Ordinal));
    }

    [Fact]
    public void CreateServiceCommand_NegativePrice_FailsValidation()
    {
        var command = new CreateServiceCommand
        {
            Name = "Valid Name",
            Duration = TimeSpan.FromMinutes(30),
            Price = -1.00m,
        };
        var results = new List<ValidationResult>();
        var context = new ValidationContext(command);

        var isValid = Validator.TryValidateObject(command, context, results, validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateServiceCommand.Price), StringComparer.Ordinal));
    }

    [Fact]
    public async Task UpdateServiceHandler_HappyPath_UpdatesAndSetsUpdatedAtUtc()
    {
        await using var db = CreateDbContext();
        var existing = CreateTestService(db);
        var handler = new UpdateServiceHandler(db);
        var command = new UpdateServiceCommand
        {
            Id = existing.Id,
            Name = "Updated Service",
            Duration = TimeSpan.FromMinutes(45),
            Price = 30.00m,
            SortOrder = 2,
        };

        var result = await handler.Handle(command);

        var response = result.AsT0;
        Assert.Equal("Updated Service", response.Name);
        Assert.NotNull(response.UpdatedAtUtc);
    }

    [Fact]
    public async Task UpdateServiceHandler_NotFound_ReturnsNotFound()
    {
        await using var db = CreateDbContext();
        var handler = new UpdateServiceHandler(db);
        var command = new UpdateServiceCommand
        {
            Id = Guid.NewGuid(),
            Name = "Any",
            Duration = TimeSpan.FromMinutes(30),
            Price = 10.00m,
        };

        var result = await handler.Handle(command);

        Assert.True(result.IsT1);
        Assert.IsType<NotFound>(result.AsT1);
    }

    [Fact]
    public async Task DeleteServiceHandler_HappyPath_DeletesAndReturnsSuccess()
    {
        await using var db = CreateDbContext();
        var existing = CreateTestService(db);
        var handler = new DeleteServiceHandler(db);

        var result = await handler.Handle(new DeleteServiceCommand(existing.Id));

        Assert.True(result.IsT0);
        Assert.IsType<Success>(result.AsT0);
        Assert.Equal(0, await db.Services.CountAsync());
    }

    [Fact]
    public async Task DeleteServiceHandler_NotFound_ReturnsNotFound()
    {
        await using var db = CreateDbContext();
        var handler = new DeleteServiceHandler(db);

        var result = await handler.Handle(new DeleteServiceCommand(Guid.NewGuid()));

        Assert.True(result.IsT1);
        Assert.IsType<NotFound>(result.AsT1);
    }

    [Fact]
    public async Task ToggleServiceActiveHandler_TogglesFromActiveToInactive()
    {
        await using var db = CreateDbContext();
        var existing = CreateTestService(db, isActive: true);
        var handler = new ToggleServiceActiveHandler(db);

        var result = await handler.Handle(new ToggleServiceActiveCommand(existing.Id));

        var response = result.AsT0;
        Assert.False(response.IsActive);
        Assert.NotNull(response.UpdatedAtUtc);
    }

    [Fact]
    public async Task ToggleServiceActiveHandler_TogglesFromInactiveToActive()
    {
        await using var db = CreateDbContext();
        var existing = CreateTestService(db, isActive: false);
        var handler = new ToggleServiceActiveHandler(db);

        var result = await handler.Handle(new ToggleServiceActiveCommand(existing.Id));

        var response = result.AsT0;
        Assert.True(response.IsActive);
    }

    [Fact]
    public async Task ToggleServiceActiveHandler_NotFound_ReturnsNotFound()
    {
        await using var db = CreateDbContext();
        var handler = new ToggleServiceActiveHandler(db);

        var result = await handler.Handle(new ToggleServiceActiveCommand(Guid.NewGuid()));

        Assert.True(result.IsT1);
        Assert.IsType<NotFound>(result.AsT1);
    }
}
