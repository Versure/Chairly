using Chairly.Domain.Enums;

namespace Chairly.Domain.Entities;

public static class BookingExtensions
{
    public static BookingStatus DeriveStatus(this Booking booking)
    {
        ArgumentNullException.ThrowIfNull(booking);

        if (booking.CancelledAtUtc != null)
        {
            return BookingStatus.Cancelled;
        }

        if (booking.NoShowAtUtc != null)
        {
            return BookingStatus.NoShow;
        }

        if (booking.CompletedAtUtc != null)
        {
            return BookingStatus.Completed;
        }

        if (booking.StartedAtUtc != null)
        {
            return BookingStatus.InProgress;
        }

        if (booking.ConfirmedAtUtc != null)
        {
            return BookingStatus.Confirmed;
        }

        return BookingStatus.Scheduled;
    }
}
