using Chairly.Api.Dispatching;

namespace Chairly.Api.Features.Services.GetServicesList;

internal static class GetServicesListEndpoint
{
    public static void MapGetServicesList(this RouteGroupBuilder group)
    {
        group.MapGet("/", async (
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new GetServicesListQuery(), cancellationToken).ConfigureAwait(false);
            return Results.Ok(result);
        });
    }
}
