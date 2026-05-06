using Chairly.Api.Shared.Mediator;
using OneOf;
using OneOf.Types;

namespace Chairly.Api.Features.Clients.GetClientTimeline;

internal sealed record GetClientTimelineQuery(Guid ClientId) : IRequest<OneOf<ClientTimelineResponse, NotFound>>;
