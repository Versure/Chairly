using Chairly.Api.Features.Notifications.Infrastructure;
using Chairly.Domain.Entities;
using Chairly.Domain.Enums;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Chairly.Tests.Features.Notifications;

public class NotificationDispatcherTests
{
    private sealed class FakeEmailSender(bool shouldFail = false) : IEmailSender
    {
        public int SendCallCount { get; private set; }
        public List<(string ToEmail, string ToName, string Subject, string HtmlBody)> SentEmails { get; } = [];

        public Task SendAsync(string toEmail, string toName, string subject, string htmlBody, CancellationToken cancellationToken)
        {
            SendCallCount++;
            SentEmails.Add((toEmail, toName, subject, htmlBody));

            if (shouldFail)
            {
                throw new InvalidOperationException("SMTP send failed");
            }

            return Task.CompletedTask;
        }
    }

    private static (NotificationDispatcher Dispatcher, string DbName, FakeEmailSender EmailSender) CreateDispatcher(bool emailShouldFail = false)
    {
        var dbName = Guid.NewGuid().ToString();
        var emailSender = new FakeEmailSender(emailShouldFail);

        var services = new ServiceCollection();
        services.AddDbContext<ChairlyDbContext>(opt => opt.UseInMemoryDatabase(dbName));
        services.AddScoped<IEmailSender>(_ => emailSender);
        var serviceProvider = services.BuildServiceProvider();
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

#pragma warning disable CA2000 // Dispatcher is the subject under test, disposed by test
        var dispatcher = new NotificationDispatcher(
            scopeFactory,
            NullLogger<NotificationDispatcher>.Instance);
#pragma warning restore CA2000

        return (dispatcher, dbName, emailSender);
    }

    private static ChairlyDbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ChairlyDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new ChairlyDbContext(options);
    }

    private static Notification SeedPendingNotification(
        string dbName,
        NotificationType type = NotificationType.BookingConfirmation,
        int retryCount = 0,
        DateTimeOffset? scheduledAt = null)
    {
        using var db = CreateDbContext(dbName);

        var tenantSettings = new TenantSettings
        {
            Id = Guid.NewGuid(),
            TenantId = TestTenantContext.DefaultTenantId,
            CompanyName = "Testsalon",
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
        db.TenantSettings.Add(tenantSettings);

        var client = new Client
        {
            Id = Guid.NewGuid(),
            TenantId = TestTenantContext.DefaultTenantId,
            FirstName = "Test",
            LastName = "Client",
            Email = "test@example.com",
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
        db.Clients.Add(client);

        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            TenantId = TestTenantContext.DefaultTenantId,
            ClientId = client.Id,
            StaffMemberId = Guid.NewGuid(),
            StartTime = DateTimeOffset.UtcNow.AddHours(1),
            EndTime = DateTimeOffset.UtcNow.AddHours(2),
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
        booking.BookingServices.Add(new BookingService
        {
            Id = Guid.NewGuid(),
            BookingId = booking.Id,
            ServiceId = Guid.NewGuid(),
            ServiceName = "Herenknippen",
            Duration = TimeSpan.FromMinutes(30),
            Price = 25.00m,
            SortOrder = 0,
        });
        db.Bookings.Add(booking);

        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            TenantId = TestTenantContext.DefaultTenantId,
            RecipientId = client.Id,
            RecipientType = RecipientType.Client,
            Channel = NotificationChannel.Email,
            Type = type,
            ReferenceId = booking.Id,
            ScheduledAtUtc = scheduledAt ?? DateTimeOffset.UtcNow.AddMinutes(-5),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            RetryCount = retryCount,
        };
        db.Notifications.Add(notification);

        db.SaveChanges();
        return notification;
    }

    private static Notification SeedPendingInvoiceNotification(string dbName)
    {
        using var db = CreateDbContext(dbName);
        var now = DateTimeOffset.UtcNow;

        var tenantSettings = new TenantSettings
        {
            Id = Guid.NewGuid(),
            TenantId = TestTenantContext.DefaultTenantId,
            CompanyName = "Salon De Spiegel",
            CreatedAtUtc = now,
        };
        db.TenantSettings.Add(tenantSettings);

        var client = new Client
        {
            Id = Guid.NewGuid(),
            TenantId = TestTenantContext.DefaultTenantId,
            FirstName = "Test",
            LastName = "Client",
            Email = "test@example.com",
            CreatedAtUtc = now,
        };
        db.Clients.Add(client);

        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            TenantId = TestTenantContext.DefaultTenantId,
            BookingId = Guid.NewGuid(),
            ClientId = client.Id,
            InvoiceNumber = "2026-0007",
            InvoiceDate = DateOnly.FromDateTime(now.UtcDateTime),
            SubTotalAmount = 40.00m,
            TotalVatAmount = 8.40m,
            TotalAmount = 48.40m,
            CreatedAtUtc = now,
            CreatedBy = Guid.NewGuid(),
        };
        db.Invoices.Add(invoice);

        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            TenantId = TestTenantContext.DefaultTenantId,
            RecipientId = client.Id,
            RecipientType = RecipientType.Client,
            Channel = NotificationChannel.Email,
            Type = NotificationType.InvoiceSent,
            ReferenceId = invoice.Id,
            ScheduledAtUtc = now.AddMinutes(-5),
            CreatedAtUtc = now,
            CreatedBy = Guid.NewGuid(),
        };
        db.Notifications.Add(notification);

        db.SaveChanges();
        return notification;
    }

    [Fact]
    public async Task Dispatcher_SetsSentAtUtc_OnSuccessfulSend()
    {
        var (dispatcher, dbName, emailSender) = CreateDispatcher();
        var notification = SeedPendingNotification(dbName);

        await dispatcher.DispatchPendingNotificationsAsync(CancellationToken.None);

        await using var db = CreateDbContext(dbName);
        var updated = await db.Notifications.FindAsync(notification.Id);
        Assert.NotNull(updated?.SentAtUtc);
        Assert.Equal(1, emailSender.SendCallCount);
    }

    [Fact]
    public async Task Dispatcher_IncrementsRetryCount_OnFailure()
    {
        var (dispatcher, dbName, _) = CreateDispatcher(emailShouldFail: true);
        SeedPendingNotification(dbName);

        await dispatcher.DispatchPendingNotificationsAsync(CancellationToken.None);

        await using var db = CreateDbContext(dbName);
        var updated = await db.Notifications.FirstAsync();
        Assert.Equal(1, updated.RetryCount);
        Assert.Null(updated.SentAtUtc);
    }

    [Fact]
    public async Task Dispatcher_SetsFailedAtUtc_After3Failures()
    {
        var (dispatcher, dbName, _) = CreateDispatcher(emailShouldFail: true);
        SeedPendingNotification(dbName, retryCount: 2);

        await dispatcher.DispatchPendingNotificationsAsync(CancellationToken.None);

        await using var db = CreateDbContext(dbName);
        var updated = await db.Notifications.FirstAsync();
        Assert.Equal(3, updated.RetryCount);
        Assert.NotNull(updated.FailedAtUtc);
        Assert.NotNull(updated.FailureReason);
    }

    [Fact]
    public async Task Dispatcher_SkipsClientWithNoEmail()
    {
        var (dispatcher, dbName, emailSender) = CreateDispatcher();

        // Seed with no email
        using var db = CreateDbContext(dbName);
        var client = new Client
        {
            Id = Guid.NewGuid(),
            TenantId = TestTenantContext.DefaultTenantId,
            FirstName = "No",
            LastName = "Email",
            Email = null,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
        db.Clients.Add(client);

        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            TenantId = TestTenantContext.DefaultTenantId,
            RecipientId = client.Id,
            RecipientType = RecipientType.Client,
            Channel = NotificationChannel.Email,
            Type = NotificationType.BookingConfirmation,
            ReferenceId = Guid.NewGuid(),
            ScheduledAtUtc = DateTimeOffset.UtcNow.AddMinutes(-5),
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
        db.Notifications.Add(notification);
        await db.SaveChangesAsync();

        await dispatcher.DispatchPendingNotificationsAsync(CancellationToken.None);

        Assert.Equal(0, emailSender.SendCallCount);
    }

    [Fact]
    public async Task Dispatcher_DoesNotPickUpFutureScheduled()
    {
        var (dispatcher, dbName, emailSender) = CreateDispatcher();
        SeedPendingNotification(dbName, scheduledAt: DateTimeOffset.UtcNow.AddHours(24));

        await dispatcher.DispatchPendingNotificationsAsync(CancellationToken.None);

        Assert.Equal(0, emailSender.SendCallCount);
    }

    [Fact]
    public async Task Dispatcher_InvoiceSent_RendersInvoiceTemplateWithDutchContent()
    {
        var (dispatcher, dbName, emailSender) = CreateDispatcher();
        SeedPendingInvoiceNotification(dbName);

        await dispatcher.DispatchPendingNotificationsAsync(CancellationToken.None);

        Assert.Single(emailSender.SentEmails);
        var sentEmail = emailSender.SentEmails[0];
        Assert.Contains("Factuur 2026-0007", sentEmail.Subject, StringComparison.Ordinal);
        Assert.Contains("Test Client", sentEmail.HtmlBody, StringComparison.Ordinal);
        Assert.Contains("Factuurnummer: 2026-0007", sentEmail.HtmlBody, StringComparison.Ordinal);
        Assert.Contains("Factuurdatum", sentEmail.HtmlBody, StringComparison.Ordinal);
        Assert.Contains("€", sentEmail.HtmlBody, StringComparison.Ordinal);
        Assert.Contains("Salon De Spiegel", sentEmail.HtmlBody, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(NotificationType.BookingConfirmation)]
    [InlineData(NotificationType.BookingReminder)]
    [InlineData(NotificationType.BookingCancellation)]
    [InlineData(NotificationType.BookingReceived)]
    public async Task Dispatcher_BookingNotificationTypes_StillDispatchCorrectly(NotificationType type)
    {
        var (dispatcher, dbName, emailSender) = CreateDispatcher();
        SeedPendingNotification(dbName, type: type);

        await dispatcher.DispatchPendingNotificationsAsync(CancellationToken.None);

        Assert.Equal(1, emailSender.SendCallCount);
        Assert.Single(emailSender.SentEmails);
        Assert.False(string.IsNullOrWhiteSpace(emailSender.SentEmails[0].Subject));
    }
}
