namespace Chairly.Api.Features.Newsletters;

internal static class NewsletterCampaignMapper
{
    public static NewsletterCampaignResponse ToResponse(Domain.Entities.NewsletterCampaign campaign)
    {
        ArgumentNullException.ThrowIfNull(campaign);
        return new NewsletterCampaignResponse(
            campaign.Id,
            campaign.Subject,
            campaign.BodyHtml,
            campaign.RecipientFilter.ToString(),
            NewsletterStatus.Derive(campaign),
            campaign.ScheduledAtUtc,
            campaign.QueuedAtUtc,
            campaign.SentAtUtc,
            campaign.CancelledAtUtc,
            campaign.CreatedAtUtc,
            campaign.CreatedBy,
            campaign.UpdatedAtUtc,
            campaign.UpdatedBy);
    }
}
