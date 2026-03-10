using Chairly.Api.Shared.Mediator;

namespace Chairly.Api.Features.Bookings.GetBookingsList;

internal static class GetBookingsListEndpoint
{
    public static void MapGetBookingsList(this RouteGroupBuilder group)
    {
        group.MapGet("/", async (
            DateOnly? date,
            Guid? staffMemberId,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new GetBookingsListQuery(date, staffMemberId), cancellationToken).ConfigureAwait(false);
            return Results.Ok(result);
        });
    }
}
