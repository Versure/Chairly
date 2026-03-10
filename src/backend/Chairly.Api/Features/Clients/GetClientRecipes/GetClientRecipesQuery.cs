using Chairly.Api.Shared.Mediator;
using OneOf;
using OneOf.Types;

namespace Chairly.Api.Features.Clients.GetClientRecipes;

internal sealed record GetClientRecipesQuery(Guid ClientId) : IRequest<OneOf<IReadOnlyList<ClientRecipeSummaryResponse>, NotFound>>;
