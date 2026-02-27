using Chairly.Api.Shared.Mediator;

namespace Chairly.Api.Features.Services.DeleteService;

internal static class DeleteServiceEndpoint
{
    public static void MapDeleteService(this RouteGroupBuilder group)
    {
        group.MapDelete("/{id:guid}", async (
            Guid id,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new DeleteServiceCommand(id), cancellationToken).ConfigureAwait(false);
            return result.Match(
                _ => Results.NoContent(),
                _ => Results.NotFound());
        });
    }
}
