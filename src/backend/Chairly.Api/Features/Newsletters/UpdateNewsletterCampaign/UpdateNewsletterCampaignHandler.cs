using Chairly.Api.Features.Newsletters.Infrastructure;
using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Results;
using Chairly.Api.Shared.Tenancy;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;

#pragma warning disable CA1812
namespace Chairly.Api.Features.Newsletters.UpdateNewsletterCampaign;

internal sealed class UpdateNewsletterCampaignHandler(
    ChairlyDbContext db,
    INewsletterHtmlSanitizer sanitizer,
    ITenantContext tenantContext) : IRequestHandler<UpdateNewsletterCampaignCommand, OneOf<NewsletterCampaignResponse, NotFound, Conflict, Unprocessable>>
{
    public async Task<OneOf<NewsletterCampaignResponse, NotFound, Conflict, Unprocessable>> Handle(UpdateNewsletterCampaignCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var campaign = await db.NewsletterCampaigns
            .FirstOrDefaultAsync(c => c.Id == command.Id && c.TenantId == tenantContext.TenantId, cancellationToken)
            .ConfigureAwait(false);

        if (campaign is null)
        {
            return new NotFound();
        }

        if (campaign.SentAtUtc is not null || campaign.CancelledAtUtc is not null || campaign.QueuedAtUtc is not null)
        {
            return new Conflict("Nieuwsbrief kan niet meer worden bewerkt.");
        }

        var sanitised = sanitizer.Sanitize(command.BodyHtml);
        if (NewsletterBodyValidator.IsEffectivelyEmpty(sanitised))
        {
            return new Unprocessable("De berichtinhoud mag niet leeg zijn.");
        }

        campaign.Subject = command.Subject;
        campaign.BodyHtml = sanitised;
        campaign.UpdatedAtUtc = DateTimeOffset.UtcNow;
        campaign.UpdatedBy = tenantContext.UserId;

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return NewsletterCampaignMapper.ToResponse(campaign);
    }
}
#pragma warning restore CA1812
