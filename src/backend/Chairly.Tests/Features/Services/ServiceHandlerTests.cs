using System.ComponentModel.DataAnnotations;
using Chairly.Api.Features.Services.CreateService;
using Chairly.Api.Features.Services.DeleteService;
using Chairly.Api.Features.Services.GetService;
using Chairly.Api.Features.Services.GetServicesList;
using Chairly.Api.Features.Services.ToggleServiceActive;
using Chairly.Api.Features.Services.UpdateService;
using Chairly.Domain.Entities;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using OneOf.Types;
using ValidationException = Chairly.Api.Shared.Mediator.ValidationException;

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
            TenantId = TestTenantContext.DefaultTenantId,
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
        var handler = new CreateServiceHandler(db, TestTenantContext.Create());
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
        var handler = new UpdateServiceHandler(db, TestTenantContext.Create());
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
        var handler = new UpdateServiceHandler(db, TestTenantContext.Create());
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
        var handler = new DeleteServiceHandler(db, TestTenantContext.Create());

        var result = await handler.Handle(new DeleteServiceCommand(existing.Id));

        Assert.True(result.IsT0);
        Assert.IsType<Success>(result.AsT0);
        Assert.Equal(0, await db.Services.CountAsync());
    }

    [Fact]
    public async Task DeleteServiceHandler_NotFound_ReturnsNotFound()
    {
        await using var db = CreateDbContext();
        var handler = new DeleteServiceHandler(db, TestTenantContext.Create());

        var result = await handler.Handle(new DeleteServiceCommand(Guid.NewGuid()));

        Assert.True(result.IsT1);
        Assert.IsType<NotFound>(result.AsT1);
    }

    [Fact]
    public async Task ToggleServiceActiveHandler_TogglesFromActiveToInactive()
    {
        await using var db = CreateDbContext();
        var existing = CreateTestService(db, isActive: true);
        var handler = new ToggleServiceActiveHandler(db, TestTenantContext.Create());

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
        var handler = new ToggleServiceActiveHandler(db, TestTenantContext.Create());

        var result = await handler.Handle(new ToggleServiceActiveCommand(existing.Id));

        var response = result.AsT0;
        Assert.True(response.IsActive);
        Assert.NotNull(response.UpdatedAtUtc);
    }

    [Fact]
    public async Task ToggleServiceActiveHandler_NotFound_ReturnsNotFound()
    {
        await using var db = CreateDbContext();
        var handler = new ToggleServiceActiveHandler(db, TestTenantContext.Create());

        var result = await handler.Handle(new ToggleServiceActiveCommand(Guid.NewGuid()));

        Assert.True(result.IsT1);
        Assert.IsType<NotFound>(result.AsT1);
    }

    [Fact]
    public async Task CreateServiceHandler_HappyPath_AssignsTenantId()
    {
        await using var db = CreateDbContext();
        var handler = new CreateServiceHandler(db, TestTenantContext.Create());
        var command = new CreateServiceCommand
        {
            Name = "Haircut",
            Duration = TimeSpan.FromMinutes(30),
            Price = 20.00m,
        };

        await handler.Handle(command);

        var entity = await db.Services.SingleAsync();
        Assert.Equal(TestTenantContext.DefaultTenantId, entity.TenantId);
    }

    [Fact]
    public async Task GetServiceHandler_HappyPath_ReturnsServiceWithCategoryName()
    {
        await using var db = CreateDbContext();
        var category = new ServiceCategory
        {
            Id = Guid.NewGuid(),
            TenantId = TestTenantContext.DefaultTenantId,
            Name = "Hair",
            SortOrder = 0,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
        db.ServiceCategories.Add(category);
        var service = new Service
        {
            Id = Guid.NewGuid(),
            TenantId = TestTenantContext.DefaultTenantId,
            Name = "Trim",
            Duration = TimeSpan.FromMinutes(15),
            Price = 10.00m,
            IsActive = true,
            SortOrder = 0,
            CategoryId = category.Id,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
        db.Services.Add(service);
        await db.SaveChangesAsync();
        var handler = new GetServiceHandler(db, TestTenantContext.Create());

        var result = await handler.Handle(new GetServiceQuery(service.Id));

        var response = result.AsT0;
        Assert.Equal("Trim", response.Name);
        Assert.Equal("Hair", response.CategoryName);
    }

    [Fact]
    public async Task GetServicesListHandler_HappyPath_ReturnsListOrderedBySortOrder()
    {
        await using var db = CreateDbContext();
        db.Services.Add(new Service
        {
            Id = Guid.NewGuid(),
            TenantId = TestTenantContext.DefaultTenantId,
            Name = "B",
            Duration = TimeSpan.FromMinutes(30),
            Price = 10.00m,
            IsActive = true,
            SortOrder = 2,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        });
        db.Services.Add(new Service
        {
            Id = Guid.NewGuid(),
            TenantId = TestTenantContext.DefaultTenantId,
            Name = "A",
            Duration = TimeSpan.FromMinutes(30),
            Price = 10.00m,
            IsActive = true,
            SortOrder = 1,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync();
        var handler = new GetServicesListHandler(db, TestTenantContext.Create());

        var result = await handler.Handle(new GetServicesListQuery());

        var list = result.ToList();
        Assert.Equal(2, list.Count);
        Assert.Equal("A", list[0].Name);
        Assert.Equal("B", list[1].Name);
    }

    [Fact]
    public async Task CreateServiceHandler_WithVatRate21_StoresVatRate()
    {
        await using var db = CreateDbContext();
        var handler = new CreateServiceHandler(db, TestTenantContext.Create());
        var command = new CreateServiceCommand
        {
            Name = "Herenknippen",
            Duration = TimeSpan.FromMinutes(30),
            Price = 25.00m,
            VatRate = 21m,
        };

        var result = await handler.Handle(command);

        Assert.Equal(21m, result.VatRate);
        var entity = await db.Services.SingleAsync();
        Assert.Equal(21m, entity.VatRate);
    }

    [Fact]
    public async Task CreateServiceHandler_WithNullVatRate_StoresNull()
    {
        await using var db = CreateDbContext();
        var handler = new CreateServiceHandler(db, TestTenantContext.Create());
        var command = new CreateServiceCommand
        {
            Name = "Herenknippen",
            Duration = TimeSpan.FromMinutes(30),
            Price = 25.00m,
            VatRate = null,
        };

        var result = await handler.Handle(command);

        Assert.Null(result.VatRate);
        var entity = await db.Services.SingleAsync();
        Assert.Null(entity.VatRate);
    }

    [Fact]
    public async Task CreateServiceHandler_WithInvalidVatRate15_ThrowsValidationException()
    {
        await using var db = CreateDbContext();
        var handler = new CreateServiceHandler(db, TestTenantContext.Create());
        var command = new CreateServiceCommand
        {
            Name = "Herenknippen",
            Duration = TimeSpan.FromMinutes(30),
            Price = 25.00m,
            VatRate = 15m,
        };

        await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(command));
    }

    [Fact]
    public async Task UpdateServiceHandler_WithVatRate9_UpdatesVatRate()
    {
        await using var db = CreateDbContext();
        var existing = CreateTestService(db);
        var handler = new UpdateServiceHandler(db, TestTenantContext.Create());
        var command = new UpdateServiceCommand
        {
            Id = existing.Id,
            Name = "Updated Service",
            Duration = TimeSpan.FromMinutes(45),
            Price = 30.00m,
            VatRate = 9m,
            SortOrder = 2,
        };

        var result = await handler.Handle(command);

        var response = result.AsT0;
        Assert.Equal(9m, response.VatRate);
    }

    [Fact]
    public async Task GetServiceHandler_ReturnsVatRateInResponse()
    {
        await using var db = CreateDbContext();
        var service = new Service
        {
            Id = Guid.NewGuid(),
            TenantId = TestTenantContext.DefaultTenantId,
            Name = "Herenknippen",
            Duration = TimeSpan.FromMinutes(30),
            Price = 25.00m,
            VatRate = 9m,
            IsActive = true,
            SortOrder = 0,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
        db.Services.Add(service);
        await db.SaveChangesAsync();
        var handler = new GetServiceHandler(db, TestTenantContext.Create());

        var result = await handler.Handle(new GetServiceQuery(service.Id));

        var response = result.AsT0;
        Assert.Equal(9m, response.VatRate);
    }
}
