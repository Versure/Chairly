using System.Text;
using System.Text.Json;
using Chairly.Api.Features.Notifications.Infrastructure;
using Chairly.Domain.Enums;
using Chairly.Domain.Events;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Chairly.Tests.Features.Notifications;

public class BookingEventConsumerTests
{
    private static ChairlyDbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ChairlyDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new ChairlyDbContext(options);
    }

    private static (BookingEventConsumer Consumer, string DbName) CreateConsumer()
    {
        var dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddDbContext<ChairlyDbContext>(opt => opt.UseInMemoryDatabase(dbName));
        var serviceProvider = services.BuildServiceProvider();
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

#pragma warning disable CA2000 // Consumer is the subject under test, disposed by test
        var consumer = new BookingEventConsumer(
            null!, // IConnection not used in HandleMessageAsync
            scopeFactory,
            NullLogger<BookingEventConsumer>.Instance);
#pragma warning restore CA2000

        return (consumer, dbName);
    }

    [Fact]
    public async Task HandleMessage_BookingCreated_CreatesReceivedAndReminder()
    {
        var (consumer, dbName) = CreateConsumer();
        var bookingEvent = new BookingCreatedEvent(
            TestTenantContext.DefaultTenantId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow.AddDays(2));

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(bookingEvent));
        await consumer.HandleMessageAsync("booking.created", body, CancellationToken.None);

        await using var db = CreateDbContext(dbName);
        var notifications = await db.Notifications.ToListAsync();
        Assert.Equal(2, notifications.Count);
        Assert.Contains(notifications, n => n.Type == NotificationType.BookingReceived);
        Assert.Contains(notifications, n => n.Type == NotificationType.BookingReminder);
    }

    [Fact]
    public async Task HandleMessage_BookingConfirmed_CreatesConfirmationOnly()
    {
        var (consumer, dbName) = CreateConsumer();
        var bookingEvent = new BookingConfirmedEvent(
            TestTenantContext.DefaultTenantId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow.AddDays(2));

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(bookingEvent));
        await consumer.HandleMessageAsync("booking.confirmed", body, CancellationToken.None);

        await using var db = CreateDbContext(dbName);
        var notifications = await db.Notifications.ToListAsync();
        Assert.Single(notifications);
        Assert.Equal(NotificationType.BookingConfirmation, notifications[0].Type);
    }

    [Fact]
    public async Task HandleMessage_BookingCancelled_CreatesCancellationNotification()
    {
        var (consumer, dbName) = CreateConsumer();
        var bookingEvent = new BookingCancelledEvent(
            TestTenantContext.DefaultTenantId,
            Guid.NewGuid(),
            Guid.NewGuid());

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(bookingEvent));
        await consumer.HandleMessageAsync("booking.cancelled", body, CancellationToken.None);

        await using var db = CreateDbContext(dbName);
        var notifications = await db.Notifications.ToListAsync();
        Assert.Single(notifications);
        Assert.Equal(NotificationType.BookingCancellation, notifications[0].Type);
    }

    [Fact]
    public async Task HandleMessage_BookingCancelled_VoidsPendingReminder()
    {
        var bookingId = Guid.NewGuid();
        var clientId = Guid.NewGuid();
        var (consumer, dbName) = CreateConsumer();

        // First create a booking created event to generate reminders
        var createdEvent = new BookingCreatedEvent(
            TestTenantContext.DefaultTenantId,
            bookingId,
            clientId,
            DateTimeOffset.UtcNow.AddDays(2));

        var createdBody = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(createdEvent));
        await consumer.HandleMessageAsync("booking.created", createdBody, CancellationToken.None);

        // Now cancel the booking
        var cancelledEvent = new BookingCancelledEvent(
            TestTenantContext.DefaultTenantId,
            bookingId,
            clientId);

        var cancelledBody = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(cancelledEvent));
        await consumer.HandleMessageAsync("booking.cancelled", cancelledBody, CancellationToken.None);

        await using var db = CreateDbContext(dbName);
        var reminders = await db.Notifications
            .Where(n => n.Type == NotificationType.BookingReminder && n.ReferenceId == bookingId)
            .ToListAsync();

        Assert.Single(reminders);
        Assert.NotNull(reminders[0].FailedAtUtc);
        Assert.Equal("Boeking geannuleerd", reminders[0].FailureReason);
    }

    [Fact]
    public async Task HandleMessage_UnknownRoutingKey_DoesNotThrow()
    {
        var (consumer, dbName) = CreateConsumer();
        var body = Encoding.UTF8.GetBytes("{}");

        // Should not throw
        await consumer.HandleMessageAsync("booking.unknown", body, CancellationToken.None);

        await using var db = CreateDbContext(dbName);
        Assert.Empty(await db.Notifications.ToListAsync());
    }

    [Fact]
    public async Task HandleMessage_BookingCreated_ReminderScheduledAtCorrectTime()
    {
        var startTime = DateTimeOffset.UtcNow.AddDays(3);
        var (consumer, dbName) = CreateConsumer();
        var bookingEvent = new BookingCreatedEvent(
            TestTenantContext.DefaultTenantId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            startTime);

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(bookingEvent));
        await consumer.HandleMessageAsync("booking.created", body, CancellationToken.None);

        await using var db = CreateDbContext(dbName);
        var reminder = await db.Notifications
            .FirstOrDefaultAsync(n => n.Type == NotificationType.BookingReminder);

        Assert.NotNull(reminder);
        var expectedSchedule = startTime.AddHours(-24);
        Assert.Equal(expectedSchedule.DateTime, reminder.ScheduledAtUtc.DateTime, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task HandleMessage_BookingCreated_SameDayBooking_DoesNotCreateReminder()
    {
        var (consumer, dbName) = CreateConsumer();
        var bookingEvent = new BookingCreatedEvent(
            TestTenantContext.DefaultTenantId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow.AddHours(2));

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(bookingEvent));
        await consumer.HandleMessageAsync("booking.created", body, CancellationToken.None);

        await using var db = CreateDbContext(dbName);
        var notifications = await db.Notifications.ToListAsync();
        Assert.Single(notifications);
        Assert.Equal(NotificationType.BookingReceived, notifications[0].Type);
    }
}
