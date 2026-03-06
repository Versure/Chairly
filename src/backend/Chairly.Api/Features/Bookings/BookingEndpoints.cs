using Chairly.Api.Features.Bookings.CreateBooking;
using Chairly.Api.Features.Bookings.GetBooking;
using Chairly.Api.Features.Bookings.GetBookingsList;

namespace Chairly.Api.Features.Bookings;

internal static class BookingEndpoints
{
    public static IEndpointRouteBuilder MapBookingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/bookings");

        group.MapGetBookingsList();
        group.MapGetBooking();
        group.MapCreateBooking();

        return app;
    }
}
