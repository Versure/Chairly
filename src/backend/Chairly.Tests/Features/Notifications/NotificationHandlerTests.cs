using Chairly.Api.Features.Notifications.GetNotificationsList;
using Chairly.Api.Shared.Tenancy;
using Chairly.Domain.Entities;
using Chairly.Domain.Enums;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Chairly.Tests.Features.Notifications;

public class NotificationHandlerTests
{
    private static ChairlyDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ChairlyDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ChairlyDbContext(options);
    }

    private static Client CreateTestClient(ChairlyDbContext db, string firstName = "Test", string lastName = "Client")
    {
        var client = new Client
        {
            Id = Guid.NewGuid(),
            TenantId = TenantConstants.DefaultTenantId,
            FirstName = firstName,
            LastName = lastName,
            Email = "test@example.com",
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
        db.Clients.Add(client);
        db.SaveChanges();
        return client;
    }

    private static Notification CreateTestNotification(
        ChairlyDbContext db,
        Guid clientId,
        NotificationType type = NotificationType.BookingConfirmation,
        DateTimeOffset? sentAtUtc = null,
        DateTimeOffset? failedAtUtc = null,
        string? failureReason = null,
        Guid? referenceId = null)
    {
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            TenantId = TenantConstants.DefaultTenantId,
            RecipientId = clientId,
            RecipientType = RecipientType.Client,
            Channel = NotificationChannel.Email,
            Type = type,
            ReferenceId = referenceId ?? Guid.NewGuid(),
            ScheduledAtUtc = DateTimeOffset.UtcNow,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            SentAtUtc = sentAtUtc,
            FailedAtUtc = failedAtUtc,
            FailureReason = failureReason,
        };
        db.Notifications.Add(notification);
        db.SaveChanges();
        return notification;
    }

    // ==================== GetNotificationsList ====================

    [Fact]
    public async Task GetNotificationsListHandler_ReturnsEmptyList_WhenNoNotifications()
    {
        await using var db = CreateDbContext();
        var handler = new GetNotificationsListHandler(db);

        var result = await handler.Handle(new GetNotificationsListQuery());

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetNotificationsListHandler_ReturnsCorrectStatusWachtend_WhenPending()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db);
        CreateTestNotification(db, client.Id);
        var handler = new GetNotificationsListHandler(db);

        var result = await handler.Handle(new GetNotificationsListQuery());

        Assert.Single(result);
        Assert.Equal("Wachtend", result[0].Status);
    }

    [Fact]
    public async Task GetNotificationsListHandler_ReturnsCorrectStatusVerzonden_WhenSent()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db);
        CreateTestNotification(db, client.Id, sentAtUtc: DateTimeOffset.UtcNow);
        var handler = new GetNotificationsListHandler(db);

        var result = await handler.Handle(new GetNotificationsListQuery());

        Assert.Single(result);
        Assert.Equal("Verzonden", result[0].Status);
    }

    [Fact]
    public async Task GetNotificationsListHandler_ReturnsCorrectStatusMislukt_WhenFailed()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db);
        CreateTestNotification(db, client.Id, failedAtUtc: DateTimeOffset.UtcNow, failureReason: "SMTP error");
        var handler = new GetNotificationsListHandler(db);

        var result = await handler.Handle(new GetNotificationsListQuery());

        Assert.Single(result);
        Assert.Equal("Mislukt", result[0].Status);
        Assert.Equal("SMTP error", result[0].FailureReason);
    }

    [Fact]
    public async Task GetNotificationsListHandler_OrdersNewestFirst()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db);

        var older = new Notification
        {
            Id = Guid.NewGuid(),
            TenantId = TenantConstants.DefaultTenantId,
            RecipientId = client.Id,
            RecipientType = RecipientType.Client,
            Channel = NotificationChannel.Email,
            Type = NotificationType.BookingConfirmation,
            ReferenceId = Guid.NewGuid(),
            ScheduledAtUtc = DateTimeOffset.UtcNow.AddHours(-2),
            CreatedAtUtc = DateTimeOffset.UtcNow.AddHours(-2),
        };

        var newer = new Notification
        {
            Id = Guid.NewGuid(),
            TenantId = TenantConstants.DefaultTenantId,
            RecipientId = client.Id,
            RecipientType = RecipientType.Client,
            Channel = NotificationChannel.Email,
            Type = NotificationType.BookingReminder,
            ReferenceId = Guid.NewGuid(),
            ScheduledAtUtc = DateTimeOffset.UtcNow,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };

        db.Notifications.Add(older);
        db.Notifications.Add(newer);
        await db.SaveChangesAsync();

        var handler = new GetNotificationsListHandler(db);

        var result = await handler.Handle(new GetNotificationsListQuery());

        Assert.Equal(2, result.Count);
        Assert.Equal(newer.Id, result[0].Id);
        Assert.Equal(older.Id, result[1].Id);
    }

    [Fact]
    public async Task GetNotificationsListHandler_ResolvesRecipientName()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db, "Jan", "de Vries");
        CreateTestNotification(db, client.Id);
        var handler = new GetNotificationsListHandler(db);

        var result = await handler.Handle(new GetNotificationsListQuery());

        Assert.Single(result);
        Assert.Equal("Jan de Vries", result[0].RecipientName);
    }

    [Fact]
    public async Task GetNotificationsListHandler_ReturnsCorrectTypeString()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db);
        CreateTestNotification(db, client.Id, NotificationType.BookingCancellation);
        var handler = new GetNotificationsListHandler(db);

        var result = await handler.Handle(new GetNotificationsListQuery());

        Assert.Single(result);
        Assert.Equal("BookingCancellation", result[0].Type);
    }

    [Fact]
    public async Task GetNotificationsListHandler_ReturnsCorrectChannelString()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db);
        CreateTestNotification(db, client.Id);
        var handler = new GetNotificationsListHandler(db);

        var result = await handler.Handle(new GetNotificationsListQuery());

        Assert.Single(result);
        Assert.Equal("Email", result[0].Channel);
    }

    [Fact(Skip = "Authorization not yet implemented")]
    public async Task GetNotificationsListHandler_StaffMemberCaller_Returns403()
    {
        // Spec B6: StaffMember callers should receive 403 Forbidden.
        // This test documents the requirement and should be unskipped
        // when role-based authorization is wired via Keycloak.
        await using var db = CreateDbContext();
        var handler = new GetNotificationsListHandler(db);

#pragma warning disable MA0026 // Inject a StaffMember principal and assert 403 response once Keycloak auth is wired
        await handler.Handle(new GetNotificationsListQuery());
#pragma warning restore MA0026
        Assert.Fail("Authorization check not yet implemented");
    }
}
