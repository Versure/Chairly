using System.Text;
using System.Text.Json;
using Chairly.Domain.Events;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Chairly.Infrastructure.Messaging;

public sealed partial class NewsletterEventPublisher(IConnection connection, ILogger<NewsletterEventPublisher> logger) : INewsletterEventPublisher
{
    public const string CampaignQueuedExchange = "chairly.newsletter.campaign-queued";
    public const string DeliveryRequestedExchange = "chairly.newsletter.delivery-requested";
    public const string TestRequestedExchange = "chairly.newsletter.test-requested";

    public async Task PublishCampaignQueuedAsync(NewsletterCampaignQueuedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        await PublishAsync(CampaignQueuedExchange, "campaign.queued", domainEvent, cancellationToken).ConfigureAwait(false);
    }

    public async Task PublishDeliveryRequestedAsync(NewsletterDeliveryRequestedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        await PublishAsync(DeliveryRequestedExchange, "delivery.requested", domainEvent, cancellationToken).ConfigureAwait(false);
    }

    public async Task PublishTestRequestedAsync(NewsletterTestRequestedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        await PublishAsync(TestRequestedExchange, "test.requested", domainEvent, cancellationToken).ConfigureAwait(false);
    }

    private async Task PublishAsync<T>(string exchange, string routingKey, T payload, CancellationToken cancellationToken)
    {
        var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        try
        {
            await channel.ExchangeDeclareAsync(
                exchange: exchange,
                type: ExchangeType.Topic,
                durable: true,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload));
            var properties = new BasicProperties
            {
                ContentType = "application/json",
                DeliveryMode = DeliveryModes.Persistent,
            };

            await channel.BasicPublishAsync(
                exchange: exchange,
                routingKey: routingKey,
                mandatory: false,
                basicProperties: properties,
                body: body,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            LogEventPublished(logger, routingKey, exchange);
        }
        finally
        {
            await channel.DisposeAsync().ConfigureAwait(false);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Published {RoutingKey} to {Exchange}")]
    private static partial void LogEventPublished(ILogger logger, string routingKey, string exchange);
}
