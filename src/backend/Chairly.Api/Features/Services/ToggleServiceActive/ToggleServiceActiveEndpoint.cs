using Chairly.Api.Shared.Mediator;

namespace Chairly.Api.Features.Services.ToggleServiceActive;

internal static class ToggleServiceActiveEndpoint
{
    public static void MapToggleServiceActive(this RouteGroupBuilder group)
    {
        group.MapPatch("/{id:guid}/toggle-active", async (
            Guid id,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new ToggleServiceActiveCommand(id), cancellationToken).ConfigureAwait(false);
            return result.Match(
                service => Results.Ok(service),
                _ => Results.NotFound());
        });
    }
}
