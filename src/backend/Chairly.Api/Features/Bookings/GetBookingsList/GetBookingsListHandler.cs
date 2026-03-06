using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Tenancy;
using Chairly.Domain.Entities;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Chairly.Api.Features.Bookings.GetBookingsList;

#pragma warning disable CA1812
internal sealed class GetBookingsListHandler(ChairlyDbContext db) : IRequestHandler<GetBookingsListQuery, IEnumerable<BookingResponse>>
{
    public async Task<IEnumerable<BookingResponse>> Handle(GetBookingsListQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var bookingsQuery = db.Bookings
            .Include(b => b.BookingServices)
            .Where(b => b.TenantId == TenantConstants.DefaultTenantId);

        if (query.Date.HasValue)
        {
            var startOfDay = new DateTimeOffset(query.Date.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc), TimeSpan.Zero);
            var endOfDay = startOfDay.AddDays(1);
            bookingsQuery = bookingsQuery.Where(b => b.StartTime >= startOfDay && b.StartTime < endOfDay);
        }

        if (query.StaffMemberId.HasValue)
        {
            bookingsQuery = bookingsQuery.Where(b => b.StaffMemberId == query.StaffMemberId.Value);
        }

        var bookings = await bookingsQuery
            .OrderBy(b => b.StartTime)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return bookings.Select(ToResponse);
    }

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
#pragma warning restore CA1812
