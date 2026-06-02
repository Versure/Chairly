using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Tenancy;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

#pragma warning disable CA1812
namespace Chairly.Api.Features.Newsletters.GetNewsletterCampaignsList;

internal sealed class GetNewsletterCampaignsListHandler(ChairlyDbContext db, ITenantContext tenantContext)
    : IRequestHandler<GetNewsletterCampaignsListQuery, IReadOnlyList<NewsletterCampaignSummaryResponse>>
{
    public async Task<IReadOnlyList<NewsletterCampaignSummaryResponse>> Handle(GetNewsletterCampaignsListQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var campaigns = await db.NewsletterCampaigns
            .Where(c => c.TenantId == tenantContext.TenantId)
            .OrderByDescending(c => c.CreatedAtUtc)
            .Include(c => c.Deliveries)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return campaigns.Select(c => new NewsletterCampaignSummaryResponse(
            c.Id,
            c.Subject,
            NewsletterStatus.Derive(c),
            c.Deliveries.Count,
            c.Deliveries.Count(d => d.SentAtUtc != null),
            c.Deliveries.Count(d => d.FailedAtUtc != null),
            c.ScheduledAtUtc,
            c.SentAtUtc,
            c.CreatedAtUtc)).ToList();
    }
}
#pragma warning restore CA1812
