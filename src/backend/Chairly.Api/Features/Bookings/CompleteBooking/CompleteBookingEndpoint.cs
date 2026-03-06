using Chairly.Api.Shared.Mediator;

namespace Chairly.Api.Features.Bookings.CompleteBooking;

internal static class CompleteBookingEndpoint
{
    public static void MapCompleteBooking(this RouteGroupBuilder group)
    {
        group.MapPost("/{id:guid}/complete", async (
            Guid id,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new CompleteBookingCommand(id), cancellationToken).ConfigureAwait(false);
            return result.Match(
                _ => Results.NoContent(),
                _ => Results.NotFound(),
                _ => Results.Conflict());
        });
    }
}
