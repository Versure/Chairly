using Chairly.Api.Shared.Mediator;

namespace Chairly.Api.Features.Bookings.GetBookingsList;

internal static class GetBookingsListEndpoint
{
    public static void MapGetBookingsList(this RouteGroupBuilder group)
    {
        group.MapGet("/", async (
            [AsParameters] GetBookingsListQuery query,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(query, cancellationToken).ConfigureAwait(false);
            return Results.Ok(result);
        });
    }
}
