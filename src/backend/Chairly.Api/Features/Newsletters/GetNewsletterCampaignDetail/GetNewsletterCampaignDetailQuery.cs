using Chairly.Api.Shared.Mediator;
using OneOf;
using OneOf.Types;

namespace Chairly.Api.Features.Newsletters.GetNewsletterCampaignDetail;

internal sealed record GetNewsletterCampaignDetailQuery(Guid Id) : IRequest<OneOf<NewsletterCampaignDetailResponse, NotFound>>;
