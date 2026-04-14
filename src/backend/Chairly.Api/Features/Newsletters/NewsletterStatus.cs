using Chairly.Domain.Entities;

namespace Chairly.Api.Features.Newsletters;

internal static class NewsletterStatus
{
    public const string Draft = "Draft";
    public const string Scheduled = "Scheduled";
    public const string Sending = "Sending";
    public const string Sent = "Sent";
    public const string Cancelled = "Cancelled";

    public static string Derive(NewsletterCampaign campaign)
    {
        ArgumentNullException.ThrowIfNull(campaign);
        if (campaign.SentAtUtc is not null)
        {
            return Sent;
        }

        if (campaign.CancelledAtUtc is not null)
        {
            return Cancelled;
        }

        if (campaign.QueuedAtUtc is not null)
        {
            return Sending;
        }

        if (campaign.ScheduledAtUtc is not null)
        {
            return Scheduled;
        }

        return Draft;
    }
}
