using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Results;
using OneOf;
using OneOf.Types;

namespace Chairly.Api.Features.Newsletters.SendNewsletterCampaign;

internal sealed record SendNewsletterCampaignCommand(Guid Id) : IRequest<OneOf<NewsletterCampaignResponse, NotFound, Conflict, Unprocessable>>;
