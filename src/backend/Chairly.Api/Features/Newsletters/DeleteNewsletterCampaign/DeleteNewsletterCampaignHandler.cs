using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Results;
using Chairly.Api.Shared.Tenancy;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;

#pragma warning disable CA1812
namespace Chairly.Api.Features.Newsletters.DeleteNewsletterCampaign;

internal sealed class DeleteNewsletterCampaignHandler(ChairlyDbContext db, ITenantContext tenantContext)
    : IRequestHandler<DeleteNewsletterCampaignCommand, OneOf<Success, NotFound, Conflict>>
{
    public async Task<OneOf<Success, NotFound, Conflict>> Handle(DeleteNewsletterCampaignCommand command, CancellationToken cancellationToken = default)
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
            return new Conflict("Alleen concept- of ingeplande nieuwsbrieven kunnen worden verwijderd.");
        }

        db.NewsletterCampaigns.Remove(campaign);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new Success();
    }
}
#pragma warning restore CA1812
