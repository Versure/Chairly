using System.Text;
using System.Text.Json;
using Chairly.Domain.Entities;
using Chairly.Domain.Enums;
using Chairly.Domain.Events;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Chairly.Api.Features.Notifications.Infrastructure;

internal sealed partial class BookingEventConsumer(
    IConnection connection,
    IServiceScopeFactory scopeFactory,
    ILogger<BookingEventConsumer> logger) : BackgroundService
{
    private const string ExchangeName = "chairly.bookings";
    private const string QueueName = "notifications.bookings";
    private const string BindingPattern = "booking.*";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken).ConfigureAwait(false);

        await channel.ExchangeDeclareAsync(
            exchange: ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            cancellationToken: stoppingToken).ConfigureAwait(false);

        await channel.QueueDeclareAsync(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken).ConfigureAwait(false);

        await channel.QueueBindAsync(
            queue: QueueName,
            exchange: ExchangeName,
            routingKey: BindingPattern,
            cancellationToken: stoppingToken).ConfigureAwait(false);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                await HandleMessageAsync(ea.RoutingKey, ea.Body, stoppingToken).ConfigureAwait(false);
                await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, stoppingToken).ConfigureAwait(false);
            }
#pragma warning disable CA1031 // Catch all to NACK and log; message will go to DLQ if configured
            catch (Exception ex)
#pragma warning restore CA1031
            {
                LogMessageHandlingFailed(logger, ea.RoutingKey, ex);
                await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, stoppingToken).ConfigureAwait(false);
            }
        };

        await channel.BasicConsumeAsync(
            queue: QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken).ConfigureAwait(false);

        // Keep alive until cancellation
        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Graceful shutdown
        }
        finally
        {
            await channel.DisposeAsync().ConfigureAwait(false);
        }
    }

    internal async Task HandleMessageAsync(string routingKey, ReadOnlyMemory<byte> body, CancellationToken cancellationToken)
    {
        var json = Encoding.UTF8.GetString(body.Span);
        var scope = scopeFactory.CreateAsyncScope();
        try
        {
            var db = scope.ServiceProvider.GetRequiredService<ChairlyDbContext>();

            switch (routingKey)
            {
                case "booking.created":
                    var created = JsonSerializer.Deserialize<BookingCreatedEvent>(json);
                    if (created is not null)
                    {
                        await HandleBookingCreatedAsync(db, created.TenantId, created.BookingId, created.ClientId, created.StartTime, cancellationToken).ConfigureAwait(false);
                    }

                    break;

                case "booking.confirmed":
                    var confirmed = JsonSerializer.Deserialize<BookingConfirmedEvent>(json);
                    if (confirmed is not null)
                    {
                        await HandleBookingConfirmedAsync(db, confirmed.TenantId, confirmed.BookingId, confirmed.ClientId, confirmed.StartTime, cancellationToken).ConfigureAwait(false);
                    }

                    break;

                case "booking.cancelled":
                    var cancelled = JsonSerializer.Deserialize<BookingCancelledEvent>(json);
                    if (cancelled is not null)
                    {
                        await HandleBookingCancelledAsync(db, cancelled.TenantId, cancelled.BookingId, cancelled.ClientId, cancellationToken).ConfigureAwait(false);
                    }

                    break;

                default:
                    LogUnknownRoutingKey(logger, routingKey);
                    break;
            }
        }
        finally
        {
            await scope.DisposeAsync().ConfigureAwait(false);
        }
    }

    private static async Task HandleBookingCreatedAsync(
        ChairlyDbContext db,
        Guid tenantId,
        Guid bookingId,
        Guid clientId,
        DateTimeOffset startTime,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        var received = new Notification
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            RecipientId = clientId,
            RecipientType = RecipientType.Client,
            Channel = NotificationChannel.Email,
            Type = NotificationType.BookingReceived,
            ReferenceId = bookingId,
            ScheduledAtUtc = now,
            CreatedAtUtc = now,
#pragma warning disable MA0026 // TODO: Replace with authenticated user ID from Keycloak (see Keycloak integration)
            CreatedBy = Guid.Empty,
#pragma warning restore MA0026
        };

        db.Notifications.Add(received);

        if (startTime.AddHours(-24) > now)
        {
            var reminder = new Notification
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                RecipientId = clientId,
                RecipientType = RecipientType.Client,
                Channel = NotificationChannel.Email,
                Type = NotificationType.BookingReminder,
                ReferenceId = bookingId,
                ScheduledAtUtc = startTime.AddHours(-24),
                CreatedAtUtc = now,
#pragma warning disable MA0026 // TODO: Replace with authenticated user ID from Keycloak (see Keycloak integration)
                CreatedBy = Guid.Empty,
#pragma warning restore MA0026
            };

            db.Notifications.Add(reminder);
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task HandleBookingConfirmedAsync(
        ChairlyDbContext db,
        Guid tenantId,
        Guid bookingId,
        Guid clientId,
        DateTimeOffset startTime,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        var confirmation = new Notification
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            RecipientId = clientId,
            RecipientType = RecipientType.Client,
            Channel = NotificationChannel.Email,
            Type = NotificationType.BookingConfirmation,
            ReferenceId = bookingId,
            ScheduledAtUtc = now,
            CreatedAtUtc = now,
#pragma warning disable MA0026 // TODO: Replace with authenticated user ID from Keycloak (see Keycloak integration)
            CreatedBy = Guid.Empty,
#pragma warning restore MA0026
        };

        db.Notifications.Add(confirmation);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task HandleBookingCancelledAsync(
        ChairlyDbContext db,
        Guid tenantId,
        Guid bookingId,
        Guid clientId,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        var cancellation = new Notification
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            RecipientId = clientId,
            RecipientType = RecipientType.Client,
            Channel = NotificationChannel.Email,
            Type = NotificationType.BookingCancellation,
            ReferenceId = bookingId,
            ScheduledAtUtc = now,
            CreatedAtUtc = now,
#pragma warning disable MA0026 // TODO: Replace with authenticated user ID from Keycloak (see Keycloak integration)
            CreatedBy = Guid.Empty,
#pragma warning restore MA0026
        };

        db.Notifications.Add(cancellation);

        // Void any pending reminders for this booking
        var pendingReminders = await db.Notifications
            .Where(n => n.TenantId == tenantId
                && n.ReferenceId == bookingId
                && n.Type == NotificationType.BookingReminder
                && n.SentAtUtc == null
                && n.FailedAtUtc == null)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var reminder in pendingReminders)
        {
            reminder.FailedAtUtc = now;
            reminder.FailureReason = "Boeking geannuleerd";
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to handle message with routing key {RoutingKey}")]
    private static partial void LogMessageHandlingFailed(ILogger logger, string routingKey, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Unknown routing key received: {RoutingKey}")]
    private static partial void LogUnknownRoutingKey(ILogger logger, string routingKey);
}
