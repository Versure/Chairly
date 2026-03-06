using Chairly.Api.Shared.Mediator;

namespace Chairly.Api.Features.Clients.CreateClient;

internal static class CreateClientEndpoint
{
    public static void MapCreateClient(this RouteGroupBuilder group)
    {
        group.MapPost("/", async (
            CreateClientCommand command,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(command, cancellationToken).ConfigureAwait(false);
            return Results.Created($"/api/clients/{result.Id}", result);
        });
    }
}
