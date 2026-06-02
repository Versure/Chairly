using System.Security.Cryptography;
using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Results;
using Chairly.Api.Shared.Tenancy;
using Chairly.Domain.Entities;
using Chairly.Domain.Events;
using Chairly.Infrastructure.Messaging;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OneOf;
using OneOf.Types;

#pragma warning disable CA1812
namespace Chairly.Api.Features.Newsletters.SendNewsletterCampaign;

internal sealed partial class SendNewsletterCampaignHandler(
    ChairlyDbContext db,
    INewsletterEventPublisher eventPublisher,
    ITenantContext tenantContext,
    ILogger<SendNewsletterCampaignHandler> logger)
    : IRequestHandler<SendNewsletterCampaignCommand, OneOf<NewsletterCampaignResponse, NotFound, Conflict, Unprocessable>>
{
    public async Task<OneOf<NewsletterCampaignResponse, NotFound, Conflict, Unprocessable>> Handle(SendNewsletterCampaignCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var campaign = await db.NewsletterCampaigns
            .FirstOrDefaultAsync(c => c.Id == command.Id && c.TenantId == tenantContext.TenantId, cancellationToken)
            .ConfigureAwait(false);

        if (campaign is null)
        {
            return new NotFound();
        }

        if (campaign.QueuedAtUtc is not null || campaign.SentAtUtc is not null || campaign.CancelledAtUtc is not null)
        {
            return new Conflict("Nieuwsbrief is al verzonden of geannuleerd.");
        }

        if (string.IsNullOrWhiteSpace(campaign.Subject) || NewsletterBodyValidator.IsEffectivelyEmpty(campaign.BodyHtml))
        {
            return new Unprocessable("Onderwerp en inhoud zijn verplicht.");
        }

        var recipients = await NewsletterRecipientLoader.LoadAsync(db, tenantContext.TenantId, cancellationToken).ConfigureAwait(false);
        if (recipients.Count == 0)
        {
            return new Unprocessable("Geen ontvangers gevonden.");
        }

        var now = DateTimeOffset.UtcNow;
        NewsletterRecipientLoader.AddDeliveries(db, campaign, recipients, now, tenantContext.UserId);
        MarkQueued(campaign, now);

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await TryPublishAsync(campaign, now, cancellationToken).ConfigureAwait(false);

        return NewsletterCampaignMapper.ToResponse(campaign);
    }

    private void MarkQueued(NewsletterCampaign campaign, DateTimeOffset now)
    {
        campaign.QueuedAtUtc = now;
        campaign.QueuedBy = tenantContext.UserId;
        campaign.ScheduledAtUtc = null;
        campaign.UpdatedAtUtc = now;
        campaign.UpdatedBy = tenantContext.UserId;
    }

    private async Task TryPublishAsync(NewsletterCampaign campaign, DateTimeOffset now, CancellationToken cancellationToken)
    {
        try
        {
            await eventPublisher.PublishCampaignQueuedAsync(
                new NewsletterCampaignQueuedEvent(campaign.TenantId, campaign.Id, now),
                cancellationToken).ConfigureAwait(false);
        }
#pragma warning disable CA1031 // Best-effort event publishing
        catch (Exception ex)
#pragma warning restore CA1031
        {
            LogPublishFailed(logger, campaign.Id, ex);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to publish queued event for campaign {CampaignId}")]
    private static partial void LogPublishFailed(ILogger logger, Guid campaignId, Exception exception);
}
#pragma warning restore CA1812
