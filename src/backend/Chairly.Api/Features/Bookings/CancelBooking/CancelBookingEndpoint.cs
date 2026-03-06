using Chairly.Api.Shared.Mediator;

namespace Chairly.Api.Features.Bookings.CancelBooking;

internal static class CancelBookingEndpoint
{
    public static void MapCancelBooking(this RouteGroupBuilder group)
    {
        group.MapPost("/{id:guid}/cancel", async (
            Guid id,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new CancelBookingCommand(id), cancellationToken).ConfigureAwait(false);
            return result.Match(
                _ => Results.NoContent(),
                _ => Results.NotFound(),
                _ => Results.Conflict());
        });
    }
}
