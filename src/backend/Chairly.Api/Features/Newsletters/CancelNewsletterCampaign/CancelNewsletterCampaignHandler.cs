using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Results;
using Chairly.Api.Shared.Tenancy;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;

#pragma warning disable CA1812
namespace Chairly.Api.Features.Newsletters.CancelNewsletterCampaign;

internal sealed class CancelNewsletterCampaignHandler(ChairlyDbContext db, ITenantContext tenantContext)
    : IRequestHandler<CancelNewsletterCampaignCommand, OneOf<NewsletterCampaignResponse, NotFound, Conflict>>
{
    public async Task<OneOf<NewsletterCampaignResponse, NotFound, Conflict>> Handle(CancelNewsletterCampaignCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var campaign = await db.NewsletterCampaigns
            .FirstOrDefaultAsync(c => c.Id == command.Id && c.TenantId == tenantContext.TenantId, cancellationToken)
            .ConfigureAwait(false);

        if (campaign is null)
        {
            return new NotFound();
        }

        if (campaign.SentAtUtc is not null)
        {
            return new Conflict("Verzonden nieuwsbrieven kunnen niet meer worden geannuleerd.");
        }

        if (campaign.CancelledAtUtc is null)
        {
            campaign.CancelledAtUtc = DateTimeOffset.UtcNow;
            campaign.CancelledBy = tenantContext.UserId;
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return NewsletterCampaignMapper.ToResponse(campaign);
    }
}
#pragma warning restore CA1812
