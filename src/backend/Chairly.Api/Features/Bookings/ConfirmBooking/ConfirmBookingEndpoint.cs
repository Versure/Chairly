using Chairly.Api.Shared.Mediator;

namespace Chairly.Api.Features.Bookings.ConfirmBooking;

internal static class ConfirmBookingEndpoint
{
    public static void MapConfirmBooking(this RouteGroupBuilder group)
    {
        group.MapPost("/{id:guid}/confirm", async (
            Guid id,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new ConfirmBookingCommand(id), cancellationToken).ConfigureAwait(false);
            return result.Match(
                _ => Results.NoContent(),
                _ => Results.NotFound(),
                _ => Results.Conflict());
        });
    }
}
