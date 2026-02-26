using Chairly.Api.Dispatching;

namespace Chairly.Api.Features.Services.GetService;

internal static class GetServiceEndpoint
{
    public static void MapGetService(this RouteGroupBuilder group)
    {
        group.MapGet("/{id:guid}", async (
            Guid id,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new GetServiceQuery(id), cancellationToken).ConfigureAwait(false);
            return result.Match(
                service => Results.Ok(service),
                _ => Results.NotFound());
        });
    }
}
