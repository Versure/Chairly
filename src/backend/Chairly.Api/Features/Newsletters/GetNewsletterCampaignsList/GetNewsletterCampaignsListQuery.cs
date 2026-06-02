using Chairly.Api.Shared.Mediator;

namespace Chairly.Api.Features.Newsletters.GetNewsletterCampaignsList;

internal sealed record GetNewsletterCampaignsListQuery : IRequest<IReadOnlyList<NewsletterCampaignSummaryResponse>>;
