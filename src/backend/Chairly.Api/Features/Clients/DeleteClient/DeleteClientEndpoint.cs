using Chairly.Api.Shared.Mediator;

namespace Chairly.Api.Features.Clients.DeleteClient;

internal static class DeleteClientEndpoint
{
    public static void MapDeleteClient(this RouteGroupBuilder group)
    {
        group.MapDelete("/{id:guid}", async (
            Guid id,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new DeleteClientCommand(id), cancellationToken).ConfigureAwait(false);
            return result.Match(
                _ => Results.NoContent(),
                _ => Results.NotFound(),
                _ => Results.Conflict());
        });
    }
}
