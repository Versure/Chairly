using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Results;
using OneOf;
using OneOf.Types;

namespace Chairly.Api.Features.Newsletters.CancelNewsletterCampaign;

internal sealed record CancelNewsletterCampaignCommand(Guid Id) : IRequest<OneOf<NewsletterCampaignResponse, NotFound, Conflict>>;
