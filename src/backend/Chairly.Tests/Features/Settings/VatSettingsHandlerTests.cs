using Chairly.Api.Features.Settings.GetVatSettings;
using Chairly.Api.Features.Settings.UpdateVatSettings;
using Chairly.Domain.Entities;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Chairly.Tests.Features.Settings;

public class VatSettingsHandlerTests
{
    private static ChairlyDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ChairlyDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ChairlyDbContext(options);
    }

    [Fact]
    public async Task GetVatSettings_NoExistingSettings_AutoCreatesWithDefault21()
    {
        await using var db = CreateDbContext();
        var handler = new GetVatSettingsHandler(db, TestTenantContext.Create());

        var result = await handler.Handle(new GetVatSettingsQuery());

        Assert.Equal(21m, result.DefaultVatRate);
        Assert.Equal(1, await db.VatSettings.CountAsync());
    }

    [Fact]
    public async Task GetVatSettings_ExistingSettings_ReturnsExistingRate()
    {
        await using var db = CreateDbContext();
        db.VatSettings.Add(new VatSettings
        {
            Id = Guid.NewGuid(),
            TenantId = TestTenantContext.DefaultTenantId,
            DefaultVatRate = 9m,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync();
        var handler = new GetVatSettingsHandler(db, TestTenantContext.Create());

        var result = await handler.Handle(new GetVatSettingsQuery());

        Assert.Equal(9m, result.DefaultVatRate);
    }

    [Fact]
    public async Task UpdateVatSettings_ValidRate_UpdatesSuccessfully()
    {
        await using var db = CreateDbContext();
        db.VatSettings.Add(new VatSettings
        {
            Id = Guid.NewGuid(),
            TenantId = TestTenantContext.DefaultTenantId,
            DefaultVatRate = 21m,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync();
        var handler = new UpdateVatSettingsHandler(db, TestTenantContext.Create());

        var result = await handler.Handle(new UpdateVatSettingsCommand { DefaultVatRate = 9m });

        Assert.Equal(9m, result.DefaultVatRate);
        var stored = await db.VatSettings.SingleAsync();
        Assert.Equal(9m, stored.DefaultVatRate);
        Assert.NotNull(stored.UpdatedAtUtc);
    }

    [Fact]
    public async Task UpdateVatSettings_NoExistingSettings_CreatesNewWithRate()
    {
        await using var db = CreateDbContext();
        var handler = new UpdateVatSettingsHandler(db, TestTenantContext.Create());

        var result = await handler.Handle(new UpdateVatSettingsCommand { DefaultVatRate = 0m });

        Assert.Equal(0m, result.DefaultVatRate);
        Assert.Equal(1, await db.VatSettings.CountAsync());
    }

    [Fact]
    public async Task UpdateVatSettings_InvalidRate_ThrowsValidationException()
    {
        await using var db = CreateDbContext();
        var handler = new UpdateVatSettingsHandler(db, TestTenantContext.Create());

        await Assert.ThrowsAsync<Api.Shared.Mediator.ValidationException>(
            () => handler.Handle(new UpdateVatSettingsCommand { DefaultVatRate = 15m }));
    }
}
