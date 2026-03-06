using Chairly.Domain.Entities;

namespace Chairly.Api.Features.Bookings;

internal static class BookingMapper
{
    internal static BookingResponse ToResponse(Booking b) => new(
        b.Id,
        b.ClientId,
        b.StaffMemberId,
        b.StartTime,
        b.EndTime,
        b.Notes,
        DeriveStatus(b),
        b.BookingServices
            .OrderBy(s => s.SortOrder)
            .Select(s => new BookingServiceResponse(s.ServiceId, s.ServiceName, s.Duration, s.Price, s.SortOrder)),
        b.CreatedAtUtc,
        b.UpdatedAtUtc,
        b.ConfirmedAtUtc,
        b.StartedAtUtc,
        b.CompletedAtUtc,
        b.CancelledAtUtc,
        b.NoShowAtUtc);

    private static string DeriveStatus(Booking b)
    {
        if (b.CancelledAtUtc != null)
        {
            return "Cancelled";
        }

        if (b.NoShowAtUtc != null)
        {
            return "NoShow";
        }

        if (b.CompletedAtUtc != null)
        {
            return "Completed";
        }

        if (b.StartedAtUtc != null)
        {
            return "InProgress";
        }

        if (b.ConfirmedAtUtc != null)
        {
            return "Confirmed";
        }

        return "Scheduled";
    }
}
