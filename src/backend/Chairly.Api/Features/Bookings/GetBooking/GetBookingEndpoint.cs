using Chairly.Api.Shared.Mediator;

namespace Chairly.Api.Features.Bookings.GetBooking;

internal static class GetBookingEndpoint
{
    public static void MapGetBooking(this RouteGroupBuilder group)
    {
        group.MapGet("/{id:guid}", async (
            Guid id,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new GetBookingQuery(id), cancellationToken).ConfigureAwait(false);
            return result.Match(
                booking => Results.Ok(booking),
                _ => Results.NotFound());
        });
    }
}
