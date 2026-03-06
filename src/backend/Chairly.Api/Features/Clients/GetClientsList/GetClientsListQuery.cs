using Chairly.Api.Shared.Mediator;

namespace Chairly.Api.Features.Clients.GetClientsList;

internal sealed record GetClientsListQuery() : IRequest<IEnumerable<ClientResponse>>;
