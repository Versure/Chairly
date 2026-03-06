using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Results;
using OneOf;
using OneOf.Types;

namespace Chairly.Api.Features.Clients.DeleteClient;

internal sealed record DeleteClientCommand(Guid Id) : IRequest<OneOf<Success, NotFound, Conflict>>;
