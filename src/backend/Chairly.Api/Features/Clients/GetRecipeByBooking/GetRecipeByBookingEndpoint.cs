using Chairly.Api.Shared.Mediator;

namespace Chairly.Api.Features.Clients.GetRecipeByBooking;

internal static class GetRecipeByBookingEndpoint
{
    public static void MapGetRecipeByBooking(this RouteGroupBuilder group)
    {
        group.MapGet("/booking/{bookingId:guid}", async (
            Guid bookingId,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new GetRecipeByBookingQuery(bookingId), cancellationToken).ConfigureAwait(false);
            return result.Match(
                recipe => Results.Ok(recipe),
                _ => Results.NotFound(),
                _ => Results.Forbid());
        });
    }
}
