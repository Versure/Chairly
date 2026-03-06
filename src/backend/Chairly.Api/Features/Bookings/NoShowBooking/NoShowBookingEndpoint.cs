using Chairly.Api.Shared.Mediator;

namespace Chairly.Api.Features.Bookings.NoShowBooking;

internal static class NoShowBookingEndpoint
{
    public static void MapNoShowBooking(this RouteGroupBuilder group)
    {
        group.MapPost("/{id:guid}/no-show", async (
            Guid id,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new NoShowBookingCommand(id), cancellationToken).ConfigureAwait(false);
            return result.Match(
                _ => Results.NoContent(),
                _ => Results.NotFound(),
                _ => Results.Conflict());
        });
    }
}
