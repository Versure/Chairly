using Chairly.Domain.Entities;

namespace Chairly.Api.Features.Bookings;

internal static class BookingMapper
{
    public static string DeriveStatus(Booking booking)
    {
        ArgumentNullException.ThrowIfNull(booking);

        if (booking.CancelledAtUtc != null)
        {
            return "Cancelled";
        }

        if (booking.NoShowAtUtc != null)
        {
            return "NoShow";
        }

        if (booking.CompletedAtUtc != null)
        {
            return "Completed";
        }

        if (booking.StartedAtUtc != null)
        {
            return "InProgress";
        }

        if (booking.ConfirmedAtUtc != null)
        {
            return "Confirmed";
        }

        return "Scheduled";
    }

    public static BookingResponse ToResponse(Booking booking)
    {
        ArgumentNullException.ThrowIfNull(booking);

        var services = booking.BookingServices
            .OrderBy(bs => bs.SortOrder)
            .Select(bs => new BookingServiceResponse(
                bs.ServiceId,
                bs.ServiceName,
                bs.Duration,
                bs.Price,
                bs.SortOrder))
            .ToList();

        return new BookingResponse(
            booking.Id,
            booking.ClientId,
            booking.StaffMemberId,
            booking.StartTime,
            booking.EndTime,
            booking.Notes,
            DeriveStatus(booking),
            services,
            booking.CreatedAtUtc,
            booking.UpdatedAtUtc,
            booking.ConfirmedAtUtc,
            booking.StartedAtUtc,
            booking.CompletedAtUtc,
            booking.CancelledAtUtc,
            booking.NoShowAtUtc);
    }
}
