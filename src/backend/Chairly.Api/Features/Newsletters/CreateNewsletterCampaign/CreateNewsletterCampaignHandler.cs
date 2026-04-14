using Chairly.Api.Features.Newsletters.Infrastructure;
using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Results;
using Chairly.Api.Shared.Tenancy;
using Chairly.Domain.Entities;
using Chairly.Infrastructure.Persistence;
using OneOf;

#pragma warning disable CA1812
namespace Chairly.Api.Features.Newsletters.CreateNewsletterCampaign;

internal sealed class CreateNewsletterCampaignHandler(
    ChairlyDbContext db,
    INewsletterHtmlSanitizer sanitizer,
    ITenantContext tenantContext) : IRequestHandler<CreateNewsletterCampaignCommand, OneOf<NewsletterCampaignResponse, Unprocessable>>
{
    public async Task<OneOf<NewsletterCampaignResponse, Unprocessable>> Handle(CreateNewsletterCampaignCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var sanitised = sanitizer.Sanitize(command.BodyHtml);
        if (NewsletterBodyValidator.IsEffectivelyEmpty(sanitised))
        {
            return new Unprocessable("De berichtinhoud mag niet leeg zijn.");
        }

        var campaign = new NewsletterCampaign
        {
            Id = Guid.NewGuid(),
            TenantId = tenantContext.TenantId,
            Subject = command.Subject,
            BodyHtml = sanitised,
            RecipientFilter = Domain.Enums.NewsletterRecipientFilter.AllSubscribed,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedBy = tenantContext.UserId,
        };

        db.NewsletterCampaigns.Add(campaign);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return NewsletterCampaignMapper.ToResponse(campaign);
    }
}
#pragma warning restore CA1812
