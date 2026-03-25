using Chairly.Api.Shared.Mediator;

namespace Chairly.Api.Features.Dashboard.GetDashboard;

internal static class GetDashboardEndpoint
{
    public static void MapGetDashboard(this RouteGroupBuilder group)
    {
        group.MapGet("/", async (
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new GetDashboardQuery(), cancellationToken).ConfigureAwait(false);
            return Results.Ok(result);
        });
    }
}
