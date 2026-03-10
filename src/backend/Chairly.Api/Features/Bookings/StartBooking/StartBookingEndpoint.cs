using Chairly.Api.Shared.Mediator;

namespace Chairly.Api.Features.Bookings.StartBooking;

internal static class StartBookingEndpoint
{
    public static void MapStartBooking(this RouteGroupBuilder group)
    {
        group.MapPost("/{id:guid}/start", async (
            Guid id,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new StartBookingCommand(id), cancellationToken).ConfigureAwait(false);
            return result.Match(
                _ => Results.NoContent(),
                _ => Results.NotFound(),
                _ => Results.Conflict());
        });
    }
}
