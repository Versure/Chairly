using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Results;
using OneOf;
using OneOf.Types;

namespace Chairly.Api.Features.Newsletters.DeleteNewsletterCampaign;

internal sealed record DeleteNewsletterCampaignCommand(Guid Id) : IRequest<OneOf<Success, NotFound, Conflict>>;
