using System.ComponentModel.DataAnnotations;
using Chairly.Api.Features.Services.CreateServiceCategory;
using Chairly.Api.Features.Services.DeleteServiceCategory;
using Chairly.Api.Features.Services.UpdateServiceCategory;
using Chairly.Api.Shared.Tenancy;
using Chairly.Domain.Entities;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using OneOf.Types;

namespace Chairly.Tests.Features.Services;

public class ServiceCategoryHandlerTests
{
    private static ChairlyDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ChairlyDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ChairlyDbContext(options);
    }

    [Fact]
    public async Task CreateServiceCategoryHandler_HappyPath_CreatesAndReturnsCategory()
    {
        await using var db = CreateDbContext();
        var handler = new CreateServiceCategoryHandler(db);
        var command = new CreateServiceCategoryCommand { Name = "Hair Services", SortOrder = 1 };

        var result = await handler.Handle(command);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("Hair Services", result.Name);
        Assert.Equal(1, result.SortOrder);
        Assert.Equal(1, await db.ServiceCategories.CountAsync());
    }

    [Fact]
    public void CreateServiceCategoryCommand_EmptyName_FailsValidation()
    {
        var command = new CreateServiceCategoryCommand { Name = string.Empty, SortOrder = 0 };
        var results = new List<ValidationResult>();
        var context = new ValidationContext(command);

        var isValid = Validator.TryValidateObject(command, context, results, validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateServiceCategoryCommand.Name), StringComparer.Ordinal));
    }

    [Fact]
    public async Task UpdateServiceCategoryHandler_HappyPath_UpdatesAndReturnsCategory()
    {
        await using var db = CreateDbContext();
        var existing = new ServiceCategory { Id = Guid.NewGuid(), TenantId = TenantConstants.DefaultTenantId, Name = "Old Name", SortOrder = 0 };
        db.ServiceCategories.Add(existing);
        await db.SaveChangesAsync();

        var handler = new UpdateServiceCategoryHandler(db);
        var command = new UpdateServiceCategoryCommand { Id = existing.Id, Name = "New Name", SortOrder = 5 };

        var result = await handler.Handle(command);

        var response = result.AsT0;
        Assert.Equal("New Name", response.Name);
        Assert.Equal(5, response.SortOrder);
    }

    [Fact]
    public async Task UpdateServiceCategoryHandler_NotFound_ReturnsNotFound()
    {
        await using var db = CreateDbContext();
        var handler = new UpdateServiceCategoryHandler(db);
        var command = new UpdateServiceCategoryCommand { Id = Guid.NewGuid(), Name = "Any", SortOrder = 0 };

        var result = await handler.Handle(command);

        Assert.True(result.IsT1);
        Assert.IsType<NotFound>(result.AsT1);
    }

    [Fact]
    public async Task DeleteServiceCategoryHandler_HappyPath_DeletesAndReturnsSuccess()
    {
        await using var db = CreateDbContext();
        var existing = new ServiceCategory { Id = Guid.NewGuid(), TenantId = TenantConstants.DefaultTenantId, Name = "Test", SortOrder = 0 };
        db.ServiceCategories.Add(existing);
        await db.SaveChangesAsync();

        var handler = new DeleteServiceCategoryHandler(db);
        var command = new DeleteServiceCategoryCommand { Id = existing.Id };

        var result = await handler.Handle(command);

        Assert.True(result.IsT0);
        Assert.IsType<Success>(result.AsT0);
        Assert.Equal(0, await db.ServiceCategories.CountAsync());
    }

    [Fact]
    public async Task DeleteServiceCategoryHandler_NotFound_ReturnsNotFound()
    {
        await using var db = CreateDbContext();
        var handler = new DeleteServiceCategoryHandler(db);
        var command = new DeleteServiceCategoryCommand { Id = Guid.NewGuid() };

        var result = await handler.Handle(command);

        Assert.True(result.IsT1);
        Assert.IsType<NotFound>(result.AsT1);
    }
}
