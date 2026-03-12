using System.Text;
using System.Text.Json;
using Chairly.Domain.Events;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Chairly.Infrastructure.Messaging;

public sealed partial class BookingEventPublisher(IConnection connection, ILogger<BookingEventPublisher> logger) : IBookingEventPublisher
{
    private const string ExchangeName = "chairly.bookings";

    public async Task PublishCreatedAsync(BookingCreatedEvent bookingEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(bookingEvent);
        await PublishAsync("booking.created", bookingEvent, cancellationToken).ConfigureAwait(false);
    }

    public async Task PublishConfirmedAsync(BookingConfirmedEvent bookingEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(bookingEvent);
        await PublishAsync("booking.confirmed", bookingEvent, cancellationToken).ConfigureAwait(false);
    }

    public async Task PublishCancelledAsync(BookingCancelledEvent bookingEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(bookingEvent);
        await PublishAsync("booking.cancelled", bookingEvent, cancellationToken).ConfigureAwait(false);
    }

    private async Task PublishAsync<T>(string routingKey, T bookingEvent, CancellationToken cancellationToken)
    {
        var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        try
        {
            await channel.ExchangeDeclareAsync(
                exchange: ExchangeName,
                type: ExchangeType.Topic,
                durable: true,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(bookingEvent));
            var properties = new BasicProperties
            {
                ContentType = "application/json",
                DeliveryMode = DeliveryModes.Persistent,
            };

            await channel.BasicPublishAsync(
                exchange: ExchangeName,
                routingKey: routingKey,
                mandatory: false,
                basicProperties: properties,
                body: body,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            LogEventPublished(logger, routingKey, ExchangeName);
        }
        finally
        {
            await channel.DisposeAsync().ConfigureAwait(false);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Published {RoutingKey} to {Exchange}")]
    private static partial void LogEventPublished(ILogger logger, string routingKey, string exchange);
}
