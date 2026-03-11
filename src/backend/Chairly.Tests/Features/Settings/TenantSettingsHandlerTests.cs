using System.ComponentModel.DataAnnotations;
using Chairly.Api.Features.Settings.GetCompanyInfo;
using Chairly.Api.Features.Settings.UpdateCompanyInfo;
using Chairly.Api.Shared.Tenancy;
using Chairly.Domain.Entities;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Chairly.Tests.Features.Settings;

public class TenantSettingsHandlerTests
{
    private static ChairlyDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ChairlyDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ChairlyDbContext(options);
    }

    private static TenantSettings CreateTestTenantSettings(ChairlyDbContext db)
    {
        var settings = new TenantSettings
        {
            Id = Guid.NewGuid(),
            TenantId = TenantConstants.DefaultTenantId,
            CompanyName = "Test Salon",
            CompanyEmail = "info@testsalon.nl",
            CompanyAddress = "Teststraat 1, 1234 AB Amsterdam",
            CompanyPhone = "0612345678",
            IbanNumber = "NL91ABNA0417164300",
            VatNumber = "NL123456789B01",
            PaymentPeriodDays = 30,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
        db.TenantSettings.Add(settings);
        db.SaveChanges();
        return settings;
    }

    [Fact]
    public async Task GetCompanyInfoHandler_NoExistingSettings_AutoCreatesEmptySettings()
    {
        await using var db = CreateDbContext();
        var handler = new GetCompanyInfoHandler(db);

        var result = await handler.Handle(new GetCompanyInfoQuery());

        Assert.Null(result.CompanyName);
        Assert.Null(result.CompanyEmail);
        Assert.Null(result.CompanyAddress);
        Assert.Null(result.CompanyPhone);
        Assert.Null(result.IbanNumber);
        Assert.Null(result.VatNumber);
        Assert.Null(result.PaymentPeriodDays);
        Assert.Equal(1, await db.TenantSettings.CountAsync());
    }

    [Fact]
    public async Task GetCompanyInfoHandler_ExistingSettings_ReturnsStoredValues()
    {
        await using var db = CreateDbContext();
        CreateTestTenantSettings(db);
        var handler = new GetCompanyInfoHandler(db);

        var result = await handler.Handle(new GetCompanyInfoQuery());

        Assert.Equal("Test Salon", result.CompanyName);
        Assert.Equal("info@testsalon.nl", result.CompanyEmail);
        Assert.Equal("Teststraat 1, 1234 AB Amsterdam", result.CompanyAddress);
        Assert.Equal("0612345678", result.CompanyPhone);
        Assert.Equal("NL91ABNA0417164300", result.IbanNumber);
        Assert.Equal("NL123456789B01", result.VatNumber);
        Assert.Equal(30, result.PaymentPeriodDays);
    }

    [Fact]
    public async Task UpdateCompanyInfoHandler_HappyPath_UpdatesAllFields()
    {
        await using var db = CreateDbContext();
        CreateTestTenantSettings(db);
        var handler = new UpdateCompanyInfoHandler(db);
        var command = new UpdateCompanyInfoCommand
        {
            CompanyName = "Updated Salon",
            CompanyEmail = "updated@salon.nl",
            CompanyAddress = "Nieuwstraat 2, 5678 CD Rotterdam",
            CompanyPhone = "0687654321",
            IbanNumber = "NL20INGB0001234567",
            VatNumber = "NL987654321B01",
            PaymentPeriodDays = 14,
        };

        var result = await handler.Handle(command);

        var response = result.AsT0;
        Assert.Equal("Updated Salon", response.CompanyName);
        Assert.Equal("updated@salon.nl", response.CompanyEmail);
        Assert.Equal("Nieuwstraat 2, 5678 CD Rotterdam", response.CompanyAddress);
        Assert.Equal("0687654321", response.CompanyPhone);
        Assert.Equal("NL20INGB0001234567", response.IbanNumber);
        Assert.Equal("NL987654321B01", response.VatNumber);
        Assert.Equal(14, response.PaymentPeriodDays);
    }

    [Fact]
    public async Task UpdateCompanyInfoHandler_NullValues_ClearsFields()
    {
        await using var db = CreateDbContext();
        CreateTestTenantSettings(db);
        var handler = new UpdateCompanyInfoHandler(db);
        var command = new UpdateCompanyInfoCommand
        {
            CompanyName = null,
            CompanyEmail = null,
            CompanyAddress = null,
            CompanyPhone = null,
            IbanNumber = null,
            VatNumber = null,
            PaymentPeriodDays = null,
        };

        var result = await handler.Handle(command);

        var response = result.AsT0;
        Assert.Null(response.CompanyName);
        Assert.Null(response.CompanyEmail);
        Assert.Null(response.CompanyAddress);
        Assert.Null(response.CompanyPhone);
        Assert.Null(response.IbanNumber);
        Assert.Null(response.VatNumber);
        Assert.Null(response.PaymentPeriodDays);
    }

    [Fact]
    public async Task UpdateCompanyInfoHandler_NoExistingSettings_AutoCreatesAndUpdates()
    {
        await using var db = CreateDbContext();
        var handler = new UpdateCompanyInfoHandler(db);
        var command = new UpdateCompanyInfoCommand
        {
            CompanyName = "New Salon",
            CompanyEmail = "new@salon.nl",
        };

        var result = await handler.Handle(command);

        var response = result.AsT0;
        Assert.Equal("New Salon", response.CompanyName);
        Assert.Equal("new@salon.nl", response.CompanyEmail);
        Assert.Equal(1, await db.TenantSettings.CountAsync());
    }

    [Fact]
    public async Task UpdateCompanyInfoHandler_SetsUpdatedAtUtc()
    {
        await using var db = CreateDbContext();
        CreateTestTenantSettings(db);
        var handler = new UpdateCompanyInfoHandler(db);
        var command = new UpdateCompanyInfoCommand
        {
            CompanyName = "Updated",
        };

        await handler.Handle(command);

        var entity = await db.TenantSettings.SingleAsync();
        Assert.NotNull(entity.UpdatedAtUtc);
    }

    [Fact]
    public void UpdateCompanyInfoCommand_InvalidEmail_FailsValidation()
    {
        var command = new UpdateCompanyInfoCommand
        {
            CompanyEmail = "not-an-email",
        };
        var results = new List<ValidationResult>();
        var context = new ValidationContext(command);

        var isValid = Validator.TryValidateObject(command, context, results, validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(UpdateCompanyInfoCommand.CompanyEmail), StringComparer.Ordinal));
    }

    [Fact]
    public void UpdateCompanyInfoCommand_PaymentPeriodDaysZero_FailsValidation()
    {
        var command = new UpdateCompanyInfoCommand
        {
            PaymentPeriodDays = 0,
        };
        var results = new List<ValidationResult>();
        var context = new ValidationContext(command);

        var isValid = Validator.TryValidateObject(command, context, results, validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(UpdateCompanyInfoCommand.PaymentPeriodDays), StringComparer.Ordinal));
    }

    [Fact]
    public void UpdateCompanyInfoCommand_PaymentPeriodDays366_FailsValidation()
    {
        var command = new UpdateCompanyInfoCommand
        {
            PaymentPeriodDays = 366,
        };
        var results = new List<ValidationResult>();
        var context = new ValidationContext(command);

        var isValid = Validator.TryValidateObject(command, context, results, validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(UpdateCompanyInfoCommand.PaymentPeriodDays), StringComparer.Ordinal));
    }

    [Fact]
    public void UpdateCompanyInfoCommand_PaymentPeriodDaysNegative_FailsValidation()
    {
        var command = new UpdateCompanyInfoCommand
        {
            PaymentPeriodDays = -1,
        };
        var results = new List<ValidationResult>();
        var context = new ValidationContext(command);

        var isValid = Validator.TryValidateObject(command, context, results, validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(UpdateCompanyInfoCommand.PaymentPeriodDays), StringComparer.Ordinal));
    }

    [Fact]
    public void UpdateCompanyInfoCommand_AllNullFields_PassesValidation()
    {
        var command = new UpdateCompanyInfoCommand();
        var results = new List<ValidationResult>();
        var context = new ValidationContext(command);

        var isValid = Validator.TryValidateObject(command, context, results, validateAllProperties: true);

        Assert.True(isValid);
        Assert.Empty(results);
    }

    [Fact]
    public void UpdateCompanyInfoCommand_CompanyNameTooLong_FailsValidation()
    {
        var command = new UpdateCompanyInfoCommand
        {
            CompanyName = new string('a', 201),
        };
        var results = new List<ValidationResult>();
        var context = new ValidationContext(command);

        var isValid = Validator.TryValidateObject(command, context, results, validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(UpdateCompanyInfoCommand.CompanyName), StringComparer.Ordinal));
    }

    [Fact]
    public void UpdateCompanyInfoCommand_IbanNumberTooLong_FailsValidation()
    {
        var command = new UpdateCompanyInfoCommand
        {
            IbanNumber = new string('A', 35),
        };
        var results = new List<ValidationResult>();
        var context = new ValidationContext(command);

        var isValid = Validator.TryValidateObject(command, context, results, validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(UpdateCompanyInfoCommand.IbanNumber), StringComparer.Ordinal));
    }
}
