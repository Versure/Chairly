using Chairly.Api.Shared.Mediator;

namespace Chairly.Api.Features.Clients.UpdateClient;

internal static class UpdateClientEndpoint
{
    public static void MapUpdateClient(this RouteGroupBuilder group)
    {
        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateClientCommand command,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            command.Id = id;
            var result = await mediator.Send(command, cancellationToken).ConfigureAwait(false);
            return result.Match(
                client => Results.Ok(client),
                _ => Results.NotFound());
        });
    }
}
