using System.ComponentModel.DataAnnotations;
using Chairly.Api.Features.Notifications.GetEmailTemplatesList;
using Chairly.Api.Features.Notifications.Infrastructure;
using Chairly.Api.Features.Notifications.PreviewEmailTemplate;
using Chairly.Api.Features.Notifications.ResetEmailTemplate;
using Chairly.Api.Features.Notifications.UpdateEmailTemplate;
using Chairly.Domain.Entities;
using Chairly.Domain.Enums;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Chairly.Tests.Features.Notifications;

public class EmailTemplateHandlerTests
{
    private static ChairlyDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ChairlyDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ChairlyDbContext(options);
    }

    private static TenantSettings CreateTestTenantSettings(ChairlyDbContext db, string companyName = "Test Salon")
    {
        var settings = new TenantSettings
        {
            Id = Guid.NewGuid(),
            TenantId = TestTenantContext.DefaultTenantId,
            CompanyName = companyName,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
        db.TenantSettings.Add(settings);
        db.SaveChanges();
        return settings;
    }

    private static EmailTemplate CreateTestEmailTemplate(
        ChairlyDbContext db,
        NotificationType type = NotificationType.BookingConfirmation,
        string subject = "Custom Subject",
        string mainMessage = "Custom Main",
        string closingMessage = "Custom Closing")
    {
        var template = new EmailTemplate
        {
            Id = Guid.NewGuid(),
            TenantId = TestTenantContext.DefaultTenantId,
            TemplateType = type,
            Subject = subject,
            MainMessage = mainMessage,
            ClosingMessage = closingMessage,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
        db.EmailTemplates.Add(template);
        db.SaveChanges();
        return template;
    }

    // ==================== B1: Entity persistence ====================

    [Fact]
    public async Task EmailTemplate_CanBePersisted_AndRetrieved()
    {
        await using var db = CreateDbContext();
        var template = CreateTestEmailTemplate(db);

        var retrieved = await db.EmailTemplates.FirstOrDefaultAsync(t => t.Id == template.Id);

        Assert.NotNull(retrieved);
        Assert.Equal("Custom Subject", retrieved.Subject);
        Assert.Equal("Custom Main", retrieved.MainMessage);
        Assert.Equal("Custom Closing", retrieved.ClosingMessage);
        Assert.Equal(NotificationType.BookingConfirmation, retrieved.TemplateType);
    }

    [Fact]
    public async Task EmailTemplate_UniqueConstraint_PreventsDoubleTenantType()
    {
        await using var db = CreateDbContext();
        CreateTestEmailTemplate(db);

        var duplicate = new EmailTemplate
        {
            Id = Guid.NewGuid(),
            TenantId = TestTenantContext.DefaultTenantId,
            TemplateType = NotificationType.BookingConfirmation,
            Subject = "Duplicate",
            MainMessage = "Duplicate",
            ClosingMessage = "Duplicate",
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
        db.EmailTemplates.Add(duplicate);

        // InMemory provider doesn't enforce unique indexes, so we verify the entity can
        // be created (the unique index is enforced at DB level in PostgreSQL).
        // We verify the configuration is correct by checking the model index.
        var entityType = db.Model.FindEntityType(typeof(EmailTemplate));
        Assert.NotNull(entityType);
        var indexes = entityType.GetIndexes().ToList();
        var uniqueIndex = indexes.Find(i => i.IsUnique);
        Assert.NotNull(uniqueIndex);
    }

    // ==================== B2: DefaultEmailTemplateValues ====================

    [Theory]
    [InlineData(NotificationType.BookingConfirmation)]
    [InlineData(NotificationType.BookingReminder)]
    [InlineData(NotificationType.BookingReceived)]
    [InlineData(NotificationType.InvoiceSent)]
    public void GetDefaults_SubjectContainsSalonName_ForRelevantTypes(NotificationType type)
    {
        var defaults = DefaultEmailTemplateValues.GetDefaults(type, "Test Salon");

        Assert.Contains("Test Salon", defaults.Subject, StringComparison.Ordinal);
    }

    [Fact]
    public void GetDefaults_BookingCancellation_SubjectDoesNotContainSalonName()
    {
        var defaults = DefaultEmailTemplateValues.GetDefaults(NotificationType.BookingCancellation, "Test Salon");

        Assert.DoesNotContain("Test Salon", defaults.Subject, StringComparison.Ordinal);
    }

    [Fact]
    public void GetDefaults_BookingCancellation_DoesNotIncludeServicesPlaceholder()
    {
        var defaults = DefaultEmailTemplateValues.GetDefaults(NotificationType.BookingCancellation, "Test");

        Assert.DoesNotContain("services", defaults.AvailablePlaceholders);
    }

    [Fact]
    public void GetDefaults_InvoiceSent_IncludesInvoicePlaceholders()
    {
        var defaults = DefaultEmailTemplateValues.GetDefaults(NotificationType.InvoiceSent, "Test");

        Assert.Contains("invoiceNumber", defaults.AvailablePlaceholders);
        Assert.Contains("invoiceDate", defaults.AvailablePlaceholders);
        Assert.Contains("totalAmount", defaults.AvailablePlaceholders);
    }

    [Theory]
    [InlineData(NotificationType.BookingConfirmation, new[] { "clientName", "salonName", "date", "services" })]
    [InlineData(NotificationType.BookingReminder, new[] { "clientName", "salonName", "date", "services" })]
    [InlineData(NotificationType.BookingCancellation, new[] { "clientName", "salonName", "date" })]
    [InlineData(NotificationType.BookingReceived, new[] { "clientName", "salonName", "date", "services" })]
    [InlineData(NotificationType.InvoiceSent, new[] { "clientName", "salonName", "invoiceNumber", "invoiceDate", "totalAmount" })]
    public void GetDefaults_ReturnsCorrectPlaceholders(NotificationType type, string[] expectedPlaceholders)
    {
        var defaults = DefaultEmailTemplateValues.GetDefaults(type, "Test");

        Assert.Equal(expectedPlaceholders, defaults.AvailablePlaceholders);
    }

    // ==================== B3: GetEmailTemplatesList ====================

    [Fact]
    public async Task GetEmailTemplatesListHandler_Returns5Templates_WhenNoCustom()
    {
        await using var db = CreateDbContext();
        CreateTestTenantSettings(db);
        var handler = new GetEmailTemplatesListHandler(db, TestTenantContext.Create());

        var result = await handler.Handle(new GetEmailTemplatesListQuery());

        Assert.Equal(5, result.Count);
        Assert.All(result, r => Assert.False(r.IsCustomized));
    }

    [Fact]
    public async Task GetEmailTemplatesListHandler_ReturnsCustomValues_WhenDbRowExists()
    {
        await using var db = CreateDbContext();
        CreateTestTenantSettings(db);
        CreateTestEmailTemplate(db, NotificationType.BookingConfirmation, "My Subject", "My Main", "My Closing");
        var handler = new GetEmailTemplatesListHandler(db, TestTenantContext.Create());

        var result = await handler.Handle(new GetEmailTemplatesListQuery());

        var confirmation = result.First(r => r.TemplateType == "BookingConfirmation");
        Assert.True(confirmation.IsCustomized);
        Assert.Equal("My Subject", confirmation.Subject);
        Assert.Equal("My Main", confirmation.MainMessage);
        Assert.Equal("My Closing", confirmation.ClosingMessage);
    }

    [Fact]
    public async Task GetEmailTemplatesListHandler_AvailablePlaceholders_CorrectPerType()
    {
        await using var db = CreateDbContext();
        CreateTestTenantSettings(db);
        var handler = new GetEmailTemplatesListHandler(db, TestTenantContext.Create());

        var result = await handler.Handle(new GetEmailTemplatesListQuery());

        var cancellation = result.First(r => r.TemplateType == "BookingCancellation");
        Assert.DoesNotContain("services", cancellation.AvailablePlaceholders);
        Assert.Contains("clientName", cancellation.AvailablePlaceholders);

        var invoice = result.First(r => r.TemplateType == "InvoiceSent");
        Assert.Contains("invoiceNumber", invoice.AvailablePlaceholders);
    }

    // ==================== B4: UpdateEmailTemplate ====================

    [Fact]
    public async Task UpdateEmailTemplateHandler_CreatesNewTemplate_ReturnsIsCustomizedTrue()
    {
        await using var db = CreateDbContext();
        var handler = new UpdateEmailTemplateHandler(db, TestTenantContext.Create());
        var command = new UpdateEmailTemplateCommand
        {
            TemplateType = "BookingConfirmation",
            Subject = "New Subject",
            MainMessage = "New Main",
            ClosingMessage = "New Closing",
        };

        var result = await handler.Handle(command);

        var response = result.AsT0;
        Assert.True(response.IsCustomized);
        Assert.Equal("New Subject", response.Subject);
        Assert.Equal(1, await db.EmailTemplates.CountAsync());
    }

    [Fact]
    public async Task UpdateEmailTemplateHandler_UpdatesExisting_SetsUpdatedAtUtc()
    {
        await using var db = CreateDbContext();
        CreateTestEmailTemplate(db);
        var handler = new UpdateEmailTemplateHandler(db, TestTenantContext.Create());
        var command = new UpdateEmailTemplateCommand
        {
            TemplateType = "BookingConfirmation",
            Subject = "Updated Subject",
            MainMessage = "Updated Main",
            ClosingMessage = "Updated Closing",
        };

        var result = await handler.Handle(command);

        var response = result.AsT0;
        Assert.Equal("Updated Subject", response.Subject);
        Assert.Equal(1, await db.EmailTemplates.CountAsync());

        var entity = await db.EmailTemplates.FirstAsync();
        Assert.NotNull(entity.UpdatedAtUtc);
    }

    [Fact]
    public async Task UpdateEmailTemplateHandler_ResponseIncludesAvailablePlaceholders()
    {
        await using var db = CreateDbContext();
        var handler = new UpdateEmailTemplateHandler(db, TestTenantContext.Create());
        var command = new UpdateEmailTemplateCommand
        {
            TemplateType = "BookingCancellation",
            Subject = "Subject",
            MainMessage = "Main",
            ClosingMessage = "Closing",
        };

        var result = await handler.Handle(command);

        var response = result.AsT0;
        Assert.Contains("clientName", response.AvailablePlaceholders);
        Assert.DoesNotContain("services", response.AvailablePlaceholders);
    }

    [Fact]
    public async Task UpdateEmailTemplateHandler_InvalidTemplateType_ReturnsBadRequest()
    {
        await using var db = CreateDbContext();
        var handler = new UpdateEmailTemplateHandler(db, TestTenantContext.Create());
        var command = new UpdateEmailTemplateCommand
        {
            TemplateType = "InvalidType",
            Subject = "Subject",
            MainMessage = "Main",
            ClosingMessage = "Closing",
        };

        var result = await handler.Handle(command);

        Assert.True(result.IsT1);
    }

    [Fact]
    public void UpdateEmailTemplateCommand_EmptySubject_FailsValidation()
    {
        var command = new UpdateEmailTemplateCommand
        {
            TemplateType = "BookingConfirmation",
            Subject = string.Empty,
            MainMessage = "Main",
            ClosingMessage = "Closing",
        };
        var results = new List<ValidationResult>();
        var context = new ValidationContext(command);

        var isValid = Validator.TryValidateObject(command, context, results, validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(UpdateEmailTemplateCommand.Subject), StringComparer.Ordinal));
    }

    [Fact]
    public void UpdateEmailTemplateCommand_SubjectExceeds500Chars_FailsValidation()
    {
        var command = new UpdateEmailTemplateCommand
        {
            TemplateType = "BookingConfirmation",
            Subject = new string('a', 501),
            MainMessage = "Main",
            ClosingMessage = "Closing",
        };
        var results = new List<ValidationResult>();
        var context = new ValidationContext(command);

        var isValid = Validator.TryValidateObject(command, context, results, validateAllProperties: true);

        Assert.False(isValid);
    }

    // ==================== B5: ResetEmailTemplate ====================

    [Fact]
    public async Task ResetEmailTemplateHandler_DeletesExisting_Returns204()
    {
        await using var db = CreateDbContext();
        CreateTestEmailTemplate(db);
        var handler = new ResetEmailTemplateHandler(db, TestTenantContext.Create());

        var result = await handler.Handle(new ResetEmailTemplateCommand("BookingConfirmation"));

        Assert.True(result.IsT0);
        Assert.Equal(0, await db.EmailTemplates.CountAsync());
    }

    [Fact]
    public async Task ResetEmailTemplateHandler_NonExisting_ReturnsSuccess()
    {
        await using var db = CreateDbContext();
        var handler = new ResetEmailTemplateHandler(db, TestTenantContext.Create());

        var result = await handler.Handle(new ResetEmailTemplateCommand("BookingConfirmation"));

        Assert.True(result.IsT0);
    }

    [Fact]
    public async Task ResetEmailTemplateHandler_InvalidType_ReturnsBadRequest()
    {
        await using var db = CreateDbContext();
        var handler = new ResetEmailTemplateHandler(db, TestTenantContext.Create());

        var result = await handler.Handle(new ResetEmailTemplateCommand("InvalidType"));

        Assert.True(result.IsT1);
    }

    [Fact]
    public async Task ResetThenGetList_ShowsDefaultsForResetType()
    {
        await using var db = CreateDbContext();
        CreateTestTenantSettings(db);
        CreateTestEmailTemplate(db);
        var resetHandler = new ResetEmailTemplateHandler(db, TestTenantContext.Create());
        var listHandler = new GetEmailTemplatesListHandler(db, TestTenantContext.Create());

        await resetHandler.Handle(new ResetEmailTemplateCommand("BookingConfirmation"));
        var result = await listHandler.Handle(new GetEmailTemplatesListQuery());

        var confirmation = result.First(r => r.TemplateType == "BookingConfirmation");
        Assert.False(confirmation.IsCustomized);
    }

    // ==================== B6: PreviewEmailTemplate ====================

    [Fact]
    public async Task PreviewEmailTemplateHandler_ReplacesPlaceholders_WithSampleData()
    {
        await using var db = CreateDbContext();
        CreateTestTenantSettings(db, "Mijn Salon");
        var handler = new PreviewEmailTemplateHandler(db, TestTenantContext.Create());
        var command = new PreviewEmailTemplateCommand
        {
            TemplateType = "BookingConfirmation",
            Subject = "Afspraak voor {clientName} bij {salonName}",
            MainMessage = "Beste {clientName}, uw afspraak is bevestigd.",
            ClosingMessage = "Tot ziens bij {salonName}!",
        };

        var result = await handler.Handle(command);

        var response = result.AsT0;
        Assert.Contains("Jan de Vries", response.Subject, StringComparison.Ordinal);
        Assert.Contains("Mijn Salon", response.Subject, StringComparison.Ordinal);
        Assert.DoesNotContain("{clientName}", response.Subject, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PreviewEmailTemplateHandler_UsesTenantCompanyName()
    {
        await using var db = CreateDbContext();
        CreateTestTenantSettings(db, "Salon Bella");
        var handler = new PreviewEmailTemplateHandler(db, TestTenantContext.Create());
        var command = new PreviewEmailTemplateCommand
        {
            TemplateType = "BookingConfirmation",
            Subject = "{salonName}",
            MainMessage = "Test",
            ClosingMessage = "Test",
        };

        var result = await handler.Handle(command);

        var response = result.AsT0;
        Assert.Equal("Salon Bella", response.Subject);
    }

    [Fact]
    public async Task PreviewEmailTemplateHandler_ReturnsValidHtml()
    {
        await using var db = CreateDbContext();
        CreateTestTenantSettings(db);
        var handler = new PreviewEmailTemplateHandler(db, TestTenantContext.Create());
        var command = new PreviewEmailTemplateCommand
        {
            TemplateType = "BookingConfirmation",
            Subject = "Test",
            MainMessage = "Test message",
            ClosingMessage = "Goodbye",
        };

        var result = await handler.Handle(command);

        var response = result.AsT0;
        Assert.Contains("<!DOCTYPE html>", response.HtmlBody, StringComparison.Ordinal);
        Assert.Contains("Test message", response.HtmlBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PreviewEmailTemplateHandler_InvalidTemplateType_ReturnsBadRequest()
    {
        await using var db = CreateDbContext();
        var handler = new PreviewEmailTemplateHandler(db, TestTenantContext.Create());
        var command = new PreviewEmailTemplateCommand
        {
            TemplateType = "InvalidType",
            Subject = "Test",
            MainMessage = "Test",
            ClosingMessage = "Test",
        };

        var result = await handler.Handle(command);

        Assert.True(result.IsT1);
    }

    [Fact]
    public void PreviewEmailTemplateCommand_EmptySubject_FailsValidation()
    {
        var command = new PreviewEmailTemplateCommand
        {
            TemplateType = "BookingConfirmation",
            Subject = string.Empty,
            MainMessage = "Main",
            ClosingMessage = "Closing",
        };
        var results = new List<ValidationResult>();
        var context = new ValidationContext(command);

        var isValid = Validator.TryValidateObject(command, context, results, validateAllProperties: true);

        Assert.False(isValid);
    }
}
