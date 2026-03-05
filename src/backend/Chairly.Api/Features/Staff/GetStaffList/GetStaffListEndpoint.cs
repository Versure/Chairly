using Chairly.Api.Shared.Mediator;

namespace Chairly.Api.Features.Staff.GetStaffList;

internal static class GetStaffListEndpoint
{
    public static void MapGetStaffList(this RouteGroupBuilder group)
    {
        group.MapGet("/", async (
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new GetStaffListQuery(), cancellationToken).ConfigureAwait(false);
            return Results.Ok(result);
        });
    }
}
