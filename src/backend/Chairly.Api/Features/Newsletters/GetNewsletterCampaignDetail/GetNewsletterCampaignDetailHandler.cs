using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Tenancy;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;

#pragma warning disable CA1812
namespace Chairly.Api.Features.Newsletters.GetNewsletterCampaignDetail;

internal sealed class GetNewsletterCampaignDetailHandler(ChairlyDbContext db, ITenantContext tenantContext)
    : IRequestHandler<GetNewsletterCampaignDetailQuery, OneOf<NewsletterCampaignDetailResponse, NotFound>>
{
    public async Task<OneOf<NewsletterCampaignDetailResponse, NotFound>> Handle(GetNewsletterCampaignDetailQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var campaign = await db.NewsletterCampaigns
            .Include(c => c.Deliveries)
            .FirstOrDefaultAsync(c => c.Id == query.Id && c.TenantId == tenantContext.TenantId, cancellationToken)
            .ConfigureAwait(false);

        if (campaign is null)
        {
            return new NotFound();
        }

        var total = campaign.Deliveries.Count;
        var sent = campaign.Deliveries.Count(d => d.SentAtUtc != null);
        var failed = campaign.Deliveries.Count(d => d.FailedAtUtc != null);
        var unsubscribed = campaign.Deliveries.Count(d => d.UnsubscribedAtUtc != null);
        var pending = total - sent - failed;

        var eligible = await db.Clients
            .CountAsync(c => c.TenantId == tenantContext.TenantId
                && c.IsSubscribedToNewsletter
                && c.DeletedAtUtc == null
                && c.Email != null
                && c.Email != string.Empty, cancellationToken)
            .ConfigureAwait(false);

        return new NewsletterCampaignDetailResponse(
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
            campaign.UpdatedAtUtc,
            total,
            sent,
            failed,
            pending,
            unsubscribed,
            eligible);
    }
}
#pragma warning restore CA1812
