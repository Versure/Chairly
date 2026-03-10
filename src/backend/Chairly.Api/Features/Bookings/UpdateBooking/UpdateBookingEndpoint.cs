using Chairly.Api.Shared.Mediator;

namespace Chairly.Api.Features.Bookings.UpdateBooking;

internal static class UpdateBookingEndpoint
{
    public static void MapUpdateBooking(this RouteGroupBuilder group)
    {
        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateBookingCommand command,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            command.Id = id;
            var result = await mediator.Send(command, cancellationToken).ConfigureAwait(false);
            return result.Match(
                booking => Results.Ok(booking),
                _ => Results.NotFound(),
                _ => Results.Conflict());
        });
    }
}
