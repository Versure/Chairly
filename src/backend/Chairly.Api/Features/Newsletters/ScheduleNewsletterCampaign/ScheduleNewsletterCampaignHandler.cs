using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Results;
using Chairly.Api.Shared.Tenancy;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;

#pragma warning disable CA1812
namespace Chairly.Api.Features.Newsletters.ScheduleNewsletterCampaign;

internal sealed class ScheduleNewsletterCampaignHandler(ChairlyDbContext db, ITenantContext tenantContext)
    : IRequestHandler<ScheduleNewsletterCampaignCommand, OneOf<NewsletterCampaignResponse, NotFound, Conflict, Unprocessable>>
{
    public async Task<OneOf<NewsletterCampaignResponse, NotFound, Conflict, Unprocessable>> Handle(ScheduleNewsletterCampaignCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.ScheduledAtUtc < DateTimeOffset.UtcNow.AddMinutes(1))
        {
            return new Unprocessable("Kies een tijdstip in de toekomst.");
        }

        var campaign = await db.NewsletterCampaigns
            .FirstOrDefaultAsync(c => c.Id == command.Id && c.TenantId == tenantContext.TenantId, cancellationToken)
            .ConfigureAwait(false);

        if (campaign is null)
        {
            return new NotFound();
        }

        if (campaign.QueuedAtUtc is not null || campaign.SentAtUtc is not null || campaign.CancelledAtUtc is not null)
        {
            return new Conflict("Alleen concepten kunnen worden ingepland.");
        }

        if (string.IsNullOrWhiteSpace(campaign.Subject) || NewsletterBodyValidator.IsEffectivelyEmpty(campaign.BodyHtml))
        {
            return new Unprocessable("Onderwerp en inhoud zijn verplicht.");
        }

        campaign.ScheduledAtUtc = command.ScheduledAtUtc;
        campaign.ScheduledBy = tenantContext.UserId;
        campaign.UpdatedAtUtc = DateTimeOffset.UtcNow;
        campaign.UpdatedBy = tenantContext.UserId;

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return NewsletterCampaignMapper.ToResponse(campaign);
    }
}
#pragma warning restore CA1812
