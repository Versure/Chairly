using System.Text;
using System.Text.Json;
using Chairly.Api.Features.Notifications.Infrastructure;
using Chairly.Domain.Events;
using Chairly.Infrastructure.Messaging;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Chairly.Api.Features.Newsletters.Infrastructure;

#pragma warning disable CA1812
internal sealed partial class NewsletterSendWorker(
    IConnection connection,
    IServiceScopeFactory scopeFactory,
    ILogger<NewsletterSendWorker> logger) : BackgroundService
{
    private const string CampaignQueue = "newsletter.campaign-queued";
    private const string DeliveryQueue = "newsletter.delivery-requested";
    private const string TestQueue = "newsletter.test-requested";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken).ConfigureAwait(false);

        await DeclareAndBindAsync(channel, NewsletterEventPublisher.CampaignQueuedExchange, CampaignQueue, "campaign.*", stoppingToken).ConfigureAwait(false);
        await DeclareAndBindAsync(channel, NewsletterEventPublisher.DeliveryRequestedExchange, DeliveryQueue, "delivery.*", stoppingToken).ConfigureAwait(false);
        await DeclareAndBindAsync(channel, NewsletterEventPublisher.TestRequestedExchange, TestQueue, "test.*", stoppingToken).ConfigureAwait(false);

        await StartConsumerAsync(channel, CampaignQueue, HandleCampaignQueuedAsync, stoppingToken).ConfigureAwait(false);
        await StartConsumerAsync(channel, DeliveryQueue, HandleDeliveryRequestedAsync, stoppingToken).ConfigureAwait(false);
        await StartConsumerAsync(channel, TestQueue, HandleTestRequestedAsync, stoppingToken).ConfigureAwait(false);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            await channel.DisposeAsync().ConfigureAwait(false);
        }
    }

    private static async Task DeclareAndBindAsync(IChannel channel, string exchange, string queue, string routingKey, CancellationToken cancellationToken)
    {
        await channel.ExchangeDeclareAsync(exchange, ExchangeType.Topic, durable: true, cancellationToken: cancellationToken).ConfigureAwait(false);
        await channel.QueueDeclareAsync(queue, durable: true, exclusive: false, autoDelete: false, cancellationToken: cancellationToken).ConfigureAwait(false);
        await channel.QueueBindAsync(queue, exchange, routingKey, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    private async Task StartConsumerAsync(IChannel channel, string queue, Func<ReadOnlyMemory<byte>, CancellationToken, Task> handler, CancellationToken cancellationToken)
    {
        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                await handler(ea.Body, cancellationToken).ConfigureAwait(false);
                await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken).ConfigureAwait(false);
            }
#pragma warning disable CA1031
            catch (Exception ex)
#pragma warning restore CA1031
            {
                LogConsumerFailed(logger, queue, ex);
                await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, cancellationToken).ConfigureAwait(false);
            }
        };

        await channel.BasicConsumeAsync(queue, autoAck: false, consumer: consumer, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    internal async Task HandleCampaignQueuedAsync(ReadOnlyMemory<byte> body, CancellationToken cancellationToken)
    {
        var json = Encoding.UTF8.GetString(body.Span);
        var domainEvent = JsonSerializer.Deserialize<NewsletterCampaignQueuedEvent>(json);
        if (domainEvent is null)
        {
            return;
        }

        var scope = scopeFactory.CreateAsyncScope();
        try
        {
            var db = scope.ServiceProvider.GetRequiredService<ChairlyDbContext>();
            var publisher = scope.ServiceProvider.GetRequiredService<INewsletterEventPublisher>();

            var campaign = await db.NewsletterCampaigns
                .Include(c => c.Deliveries)
                .FirstOrDefaultAsync(c => c.Id == domainEvent.CampaignId, cancellationToken)
                .ConfigureAwait(false);

            if (campaign is null || campaign.CancelledAtUtc is not null)
            {
                return;
            }

            var pending = campaign.Deliveries
                .Where(d => d.SentAtUtc is null && d.FailedAtUtc is null && d.UnsubscribedAtUtc is null)
                .ToList();

            foreach (var delivery in pending)
            {
                await publisher.PublishDeliveryRequestedAsync(
                    new NewsletterDeliveryRequestedEvent(campaign.TenantId, campaign.Id, delivery.Id),
                    cancellationToken).ConfigureAwait(false);
            }

            campaign.SentAtUtc = DateTimeOffset.UtcNow;
            campaign.SentBy = campaign.QueuedBy;
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            await scope.DisposeAsync().ConfigureAwait(false);
        }
    }

    internal async Task HandleDeliveryRequestedAsync(ReadOnlyMemory<byte> body, CancellationToken cancellationToken)
    {
        var json = Encoding.UTF8.GetString(body.Span);
        var domainEvent = JsonSerializer.Deserialize<NewsletterDeliveryRequestedEvent>(json);
        if (domainEvent is null)
        {
            return;
        }

        var scope = scopeFactory.CreateAsyncScope();
        try
        {
            var db = scope.ServiceProvider.GetRequiredService<ChairlyDbContext>();
            var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();

            var delivery = await db.NewsletterDeliveries
                .FirstOrDefaultAsync(d => d.Id == domainEvent.DeliveryId, cancellationToken)
                .ConfigureAwait(false);
            if (delivery is null || delivery.SentAtUtc is not null || delivery.FailedAtUtc is not null || delivery.UnsubscribedAtUtc is not null)
            {
                return;
            }

            var campaign = await db.NewsletterCampaigns
                .FirstOrDefaultAsync(c => c.Id == delivery.CampaignId, cancellationToken)
                .ConfigureAwait(false);
            if (campaign is null || campaign.CancelledAtUtc is not null)
            {
                return;
            }

            var settings = await db.TenantSettings
                .FirstOrDefaultAsync(s => s.TenantId == campaign.TenantId, cancellationToken)
                .ConfigureAwait(false);
            var salonName = settings?.CompanyName ?? "Uw salon";
            var unsubscribeUrl = $"/api/newsletters/unsubscribe/{delivery.UnsubscribeToken}";

            var html = NewsletterRenderer.Render(campaign.BodyHtml, salonName, unsubscribeUrl);

            try
            {
                await emailSender.SendAsync(delivery.Email, delivery.Email, campaign.Subject, html, attachment: null, cancellationToken).ConfigureAwait(false);
                delivery.SentAtUtc = DateTimeOffset.UtcNow;
            }
#pragma warning disable CA1031
            catch (Exception ex)
#pragma warning restore CA1031
            {
                delivery.FailedAtUtc = DateTimeOffset.UtcNow;
                delivery.FailureReason = ex.Message.Length > 1000 ? ex.Message[..1000] : ex.Message;
            }

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            await scope.DisposeAsync().ConfigureAwait(false);
        }
    }

    internal async Task HandleTestRequestedAsync(ReadOnlyMemory<byte> body, CancellationToken cancellationToken)
    {
        var json = Encoding.UTF8.GetString(body.Span);
        var domainEvent = JsonSerializer.Deserialize<NewsletterTestRequestedEvent>(json);
        if (domainEvent is null)
        {
            return;
        }

        var scope = scopeFactory.CreateAsyncScope();
        try
        {
            var db = scope.ServiceProvider.GetRequiredService<ChairlyDbContext>();
            var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();

            var campaign = await db.NewsletterCampaigns
                .FirstOrDefaultAsync(c => c.Id == domainEvent.CampaignId && c.TenantId == domainEvent.TenantId, cancellationToken)
                .ConfigureAwait(false);
            if (campaign is null)
            {
                return;
            }

            var settings = await db.TenantSettings
                .FirstOrDefaultAsync(s => s.TenantId == campaign.TenantId, cancellationToken)
                .ConfigureAwait(false);
            var salonName = settings?.CompanyName ?? "Uw salon";

            var html = NewsletterRenderer.Render(campaign.BodyHtml, salonName, "#test-send");
            var subject = "[TEST] " + campaign.Subject;

            await emailSender.SendAsync(domainEvent.RecipientEmail, domainEvent.RecipientEmail, subject, html, attachment: null, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            await scope.DisposeAsync().ConfigureAwait(false);
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Newsletter consumer failed for queue {Queue}")]
    private static partial void LogConsumerFailed(ILogger logger, string queue, Exception exception);
}
#pragma warning restore CA1812
