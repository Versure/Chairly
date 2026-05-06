using Chairly.Api.Shared.Mediator;

namespace Chairly.Api.Features.Clients.GetClientTimeline;

internal static class GetClientTimelineEndpoint
{
    public static void MapGetClientTimeline(this RouteGroupBuilder group)
    {
        group.MapGet("/{clientId:guid}/timeline", async (
            Guid clientId,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new GetClientTimelineQuery(clientId), cancellationToken).ConfigureAwait(false);
            return result.Match(
                timeline => Results.Ok(timeline),
                _ => Results.NotFound());
        });
    }
}
