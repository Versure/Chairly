using Chairly.Api.Shared.Mediator;

namespace Chairly.Api.Features.Clients.GetClientRecipes;

internal static class GetClientRecipesEndpoint
{
    public static void MapGetClientRecipes(this RouteGroupBuilder group)
    {
        group.MapGet("/{clientId:guid}/recipes", async (
            Guid clientId,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new GetClientRecipesQuery(clientId), cancellationToken).ConfigureAwait(false);
            return result.Match(
                recipes => Results.Ok(recipes),
                _ => Results.NotFound());
        });
    }
}
