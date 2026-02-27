using Chairly.Api.Shared.Mediator;

namespace Chairly.Api.Features.Services.UpdateService;

internal static class UpdateServiceEndpoint
{
    public static void MapUpdateService(this RouteGroupBuilder group)
    {
        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateServiceCommand command,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            command.Id = id;
            var result = await mediator.Send(command, cancellationToken).ConfigureAwait(false);
            return result.Match(
                service => Results.Ok(service),
                _ => Results.NotFound());
        });
    }
}
