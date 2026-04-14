using Chairly.Domain.Entities;
using Chairly.Domain.Events;
using Chairly.Infrastructure.Messaging;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Chairly.Api.Features.Newsletters.Infrastructure;

#pragma warning disable CA1812
internal sealed partial class NewsletterSchedulerHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<NewsletterSchedulerHostedService> logger) : BackgroundService
{
    private const int PollIntervalSeconds = 60;

#pragma warning disable MA0026 // System user — scheduler runs without authenticated user
    private static Guid SystemUserId() => Guid.Empty;
#pragma warning restore MA0026

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessDueCampaignsAsync(stoppingToken).ConfigureAwait(false);
            }
#pragma warning disable CA1031
            catch (Exception ex) when (ex is not OperationCanceledException)
#pragma warning restore CA1031
            {
                LogCycleFailed(logger, ex);
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(PollIntervalSeconds), stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    internal async Task ProcessDueCampaignsAsync(CancellationToken cancellationToken)
    {
        var scope = scopeFactory.CreateAsyncScope();
        try
        {
            var db = scope.ServiceProvider.GetRequiredService<ChairlyDbContext>();
            var publisher = scope.ServiceProvider.GetRequiredService<INewsletterEventPublisher>();
            var due = await LoadDueCampaignsAsync(db, cancellationToken).ConfigureAwait(false);

            foreach (var campaign in due)
            {
                await ProcessOneAsync(db, publisher, campaign, cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            await scope.DisposeAsync().ConfigureAwait(false);
        }
    }

    private static Task<List<NewsletterCampaign>> LoadDueCampaignsAsync(ChairlyDbContext db, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        return db.NewsletterCampaigns
            .Where(c => c.ScheduledAtUtc != null
                && c.ScheduledAtUtc <= now
                && c.QueuedAtUtc == null
                && c.SentAtUtc == null
                && c.CancelledAtUtc == null)
            .ToListAsync(cancellationToken);
    }

    private async Task ProcessOneAsync(ChairlyDbContext db, INewsletterEventPublisher publisher, NewsletterCampaign campaign, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(campaign.Subject) || NewsletterBodyValidator.IsEffectivelyEmpty(campaign.BodyHtml))
        {
            LogSkipped(logger, campaign.Id);
            return;
        }

        var recipients = await NewsletterRecipientLoader.LoadAsync(db, campaign.TenantId, cancellationToken).ConfigureAwait(false);
        if (recipients.Count == 0)
        {
            LogSkipped(logger, campaign.Id);
            return;
        }

        var now = DateTimeOffset.UtcNow;
        NewsletterRecipientLoader.AddDeliveries(db, campaign, recipients, now, SystemUserId());
        campaign.QueuedAtUtc = now;
        campaign.QueuedBy = SystemUserId();
        campaign.UpdatedAtUtc = now;
        campaign.UpdatedBy = SystemUserId();

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            await publisher.PublishCampaignQueuedAsync(
                new NewsletterCampaignQueuedEvent(campaign.TenantId, campaign.Id, now),
                cancellationToken).ConfigureAwait(false);
        }
#pragma warning disable CA1031
        catch (Exception ex)
#pragma warning restore CA1031
        {
            LogPublishFailed(logger, campaign.Id, ex);
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Newsletter scheduler cycle failed")]
    private static partial void LogCycleFailed(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Information, Message = "Skipped scheduled newsletter campaign {CampaignId}")]
    private static partial void LogSkipped(ILogger logger, Guid campaignId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to publish queued event for campaign {CampaignId}")]
    private static partial void LogPublishFailed(ILogger logger, Guid campaignId, Exception exception);
}
#pragma warning restore CA1812
