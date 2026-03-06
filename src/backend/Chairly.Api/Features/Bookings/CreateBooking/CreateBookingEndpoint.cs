using Chairly.Api.Shared.Mediator;

namespace Chairly.Api.Features.Bookings.CreateBooking;

internal static class CreateBookingEndpoint
{
    public static void MapCreateBooking(this RouteGroupBuilder group)
    {
        group.MapPost("/", async (
            CreateBookingCommand command,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(command, cancellationToken).ConfigureAwait(false);
            return result.Match(
                booking => Results.Created($"/api/bookings/{booking.Id}", booking),
                _ => Results.NotFound(),
                _ => Results.Conflict());
        });
    }
}
