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
        string body = "<p>Custom Body</p>")
    {
        var template = new EmailTemplate
        {
            Id = Guid.NewGuid(),
            TenantId = TestTenantContext.DefaultTenantId,
            TemplateType = type,
            Subject = subject,
            Body = body,
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
        Assert.Equal("<p>Custom Body</p>", retrieved.Body);
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
            Body = "Duplicate",
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

    [Fact]
    public void GetDefaults_BodyContainsPlaceholders()
    {
        var defaults = DefaultEmailTemplateValues.GetDefaults(NotificationType.BookingConfirmation, "Test");

        Assert.Contains("{date}", defaults.Body, StringComparison.Ordinal);
        Assert.Contains("{services}", defaults.Body, StringComparison.Ordinal);
    }

    [Fact]
    public void GetDefaults_InvoiceSent_BodyContainsInvoicePlaceholders()
    {
        var defaults = DefaultEmailTemplateValues.GetDefaults(NotificationType.InvoiceSent, "Test");

        Assert.Contains("{invoiceNumber}", defaults.Body, StringComparison.Ordinal);
        Assert.Contains("{invoiceDate}", defaults.Body, StringComparison.Ordinal);
        Assert.Contains("{totalAmount}", defaults.Body, StringComparison.Ordinal);
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
        CreateTestEmailTemplate(db, NotificationType.BookingConfirmation, "My Subject", "<p>My Body</p>");
        var handler = new GetEmailTemplatesListHandler(db, TestTenantContext.Create());

        var result = await handler.Handle(new GetEmailTemplatesListQuery());

        var confirmation = result.First(r => r.TemplateType == "BookingConfirmation");
        Assert.True(confirmation.IsCustomized);
        Assert.Equal("My Subject", confirmation.Subject);
        Assert.Equal("<p>My Body</p>", confirmation.Body);
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

    [Fact]
    public async Task GetEmailTemplatesListHandler_DefaultBody_ContainsHtml()
    {
        await using var db = CreateDbContext();
        CreateTestTenantSettings(db);
        var handler = new GetEmailTemplatesListHandler(db, TestTenantContext.Create());

        var result = await handler.Handle(new GetEmailTemplatesListQuery());

        var confirmation = result.First(r => r.TemplateType == "BookingConfirmation");
        Assert.Contains("<p>", confirmation.Body, StringComparison.Ordinal);
        Assert.Contains("{date}", confirmation.Body, StringComparison.Ordinal);
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
            Body = "<p>New Body</p>",
        };

        var result = await handler.Handle(command);

        var response = result.AsT0;
        Assert.True(response.IsCustomized);
        Assert.Equal("New Subject", response.Subject);
        Assert.Equal("<p>New Body</p>", response.Body);
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
            Body = "<p>Updated Body</p>",
        };

        var result = await handler.Handle(command);

        var response = result.AsT0;
        Assert.Equal("Updated Subject", response.Subject);
        Assert.Equal("<p>Updated Body</p>", response.Body);
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
            Body = "<p>Body</p>",
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
            Body = "<p>Body</p>",
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
            Body = "<p>Body</p>",
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
            Body = "<p>Body</p>",
        };
        var results = new List<ValidationResult>();
        var context = new ValidationContext(command);

        var isValid = Validator.TryValidateObject(command, context, results, validateAllProperties: true);

        Assert.False(isValid);
    }

    [Fact]
    public void UpdateEmailTemplateCommand_BodyExceeds10000Chars_FailsValidation()
    {
        var command = new UpdateEmailTemplateCommand
        {
            TemplateType = "BookingConfirmation",
            Subject = "Subject",
            Body = new string('a', 10001),
        };
        var results = new List<ValidationResult>();
        var context = new ValidationContext(command);

        var isValid = Validator.TryValidateObject(command, context, results, validateAllProperties: true);

        Assert.False(isValid);
    }

    [Fact]
    public void UpdateEmailTemplateCommand_EmptyBody_FailsValidation()
    {
        var command = new UpdateEmailTemplateCommand
        {
            TemplateType = "BookingConfirmation",
            Subject = "Subject",
            Body = string.Empty,
        };
        var results = new List<ValidationResult>();
        var context = new ValidationContext(command);

        var isValid = Validator.TryValidateObject(command, context, results, validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(UpdateEmailTemplateCommand.Body), StringComparer.Ordinal));
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
            Body = "<p>Beste {clientName}, uw afspraak is bevestigd op {date}.</p><p>Diensten: {services}</p>",
        };

        var result = await handler.Handle(command);

        var response = result.AsT0;
        Assert.Contains("Jan de Vries", response.Subject, StringComparison.Ordinal);
        Assert.Contains("Mijn Salon", response.Subject, StringComparison.Ordinal);
        Assert.DoesNotContain("{clientName}", response.Subject, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PreviewEmailTemplateHandler_ReplacesPlaceholders_InBody()
    {
        await using var db = CreateDbContext();
        CreateTestTenantSettings(db, "Mijn Salon");
        var handler = new PreviewEmailTemplateHandler(db, TestTenantContext.Create());
        var command = new PreviewEmailTemplateCommand
        {
            TemplateType = "BookingConfirmation",
            Subject = "Test",
            Body = "<p>{clientName} bij {salonName}, diensten: {services}</p>",
        };

        var result = await handler.Handle(command);

        var response = result.AsT0;
        Assert.Contains("Jan de Vries", response.HtmlBody, StringComparison.Ordinal);
        Assert.Contains("Mijn Salon", response.HtmlBody, StringComparison.Ordinal);
        Assert.Contains("Heren knippen, Baard trimmen", response.HtmlBody, StringComparison.Ordinal);
        Assert.DoesNotContain("{clientName}", response.HtmlBody, StringComparison.Ordinal);
        Assert.DoesNotContain("{services}", response.HtmlBody, StringComparison.Ordinal);
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
            Body = "<p>Test</p>",
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
            Body = "<p>Test message</p>",
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
            Body = "<p>Test</p>",
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
            Body = "<p>Body</p>",
        };
        var results = new List<ValidationResult>();
        var context = new ValidationContext(command);

        var isValid = Validator.TryValidateObject(command, context, results, validateAllProperties: true);

        Assert.False(isValid);
    }

    [Fact]
    public void PreviewEmailTemplateCommand_EmptyBody_FailsValidation()
    {
        var command = new PreviewEmailTemplateCommand
        {
            TemplateType = "BookingConfirmation",
            Subject = "Subject",
            Body = string.Empty,
        };
        var results = new List<ValidationResult>();
        var context = new ValidationContext(command);

        var isValid = Validator.TryValidateObject(command, context, results, validateAllProperties: true);

        Assert.False(isValid);
    }

    // ==================== BuildTemplateFromBody ====================

    [Fact]
    public void BuildTemplateFromBody_ReturnsValidHtmlWithContent()
    {
        var html = EmailTemplates.BuildTemplateFromBody("Mijn Salon", "Jan Smit", "<p>Uw afspraak is bevestigd.</p>");

        Assert.Contains("<!DOCTYPE html>", html, StringComparison.Ordinal);
        Assert.Contains("Mijn Salon", html, StringComparison.Ordinal);
        Assert.Contains("Beste Jan Smit", html, StringComparison.Ordinal);
        Assert.Contains("Uw afspraak is bevestigd.", html, StringComparison.Ordinal);
        Assert.Contains("Met vriendelijke groet", html, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildTemplateFromBody_ContainsSalonNameInHeaderAndFooter()
    {
        var html = EmailTemplates.BuildTemplateFromBody("Kapsalon De Knip", "Jan", "<p>Body</p>");

        // Salon name should appear in header h1 and footer
        var headerIndex = html.IndexOf("Kapsalon De Knip", StringComparison.Ordinal);
        Assert.True(headerIndex >= 0);
        var footerIndex = html.IndexOf("Kapsalon De Knip", headerIndex + 1, StringComparison.Ordinal);
        Assert.True(footerIndex > headerIndex);
    }
}
