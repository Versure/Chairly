using Chairly.Api.Shared.Mediator;

namespace Chairly.Api.Features.Clients.GetClientsList;

internal static class GetClientsListEndpoint
{
    public static void MapGetClientsList(this RouteGroupBuilder group)
    {
        group.MapGet("/", async (
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new GetClientsListQuery(), cancellationToken).ConfigureAwait(false);
            return Results.Ok(result);
        });
    }
}
