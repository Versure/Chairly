using Chairly.Api.Features.Bookings.CancelBooking;
using Chairly.Api.Features.Bookings.CompleteBooking;
using Chairly.Api.Features.Bookings.ConfirmBooking;
using Chairly.Api.Features.Bookings.CreateBooking;
using Chairly.Api.Features.Bookings.GetBooking;
using Chairly.Api.Features.Bookings.GetBookingsList;
using Chairly.Api.Features.Bookings.MarkBookingNoShow;
using Chairly.Api.Features.Bookings.StartBooking;
using Chairly.Api.Features.Bookings.UpdateBooking;

namespace Chairly.Api.Features.Bookings;

internal static class BookingEndpoints
{
    public static IEndpointRouteBuilder MapBookingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/bookings")
            .RequireAuthorization("RequireStaff");

        group.MapGetBookingsList();
        group.MapGetBooking();
        group.MapCreateBooking();
        group.MapUpdateBooking();
        group.MapCancelBooking();
        group.MapConfirmBooking();
        group.MapStartBooking();
        group.MapCompleteBooking();
        group.MapMarkBookingNoShow();

        return app;
    }
}
