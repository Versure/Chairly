using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Tenancy;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Chairly.Api.Features.Bookings.GetBookingsList;

#pragma warning disable CA1812
internal sealed class GetBookingsListHandler(ChairlyDbContext db) : IRequestHandler<GetBookingsListQuery, IReadOnlyList<BookingResponse>>
{
    public async Task<IReadOnlyList<BookingResponse>> Handle(GetBookingsListQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var queryable = db.Bookings
            .Include(b => b.BookingServices)
            .Where(b => b.TenantId == TenantConstants.DefaultTenantId);

        if (query.Date.HasValue)
        {
            var dateStart = query.Date.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var dateEnd = query.Date.Value.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var dateStartOffset = new DateTimeOffset(dateStart, TimeSpan.Zero);
            var dateEndOffset = new DateTimeOffset(dateEnd, TimeSpan.Zero);
            queryable = queryable.Where(b => b.StartTime >= dateStartOffset && b.StartTime < dateEndOffset);
        }

        if (query.StaffMemberId.HasValue)
        {
            queryable = queryable.Where(b => b.StaffMemberId == query.StaffMemberId.Value);
        }

        var bookings = await queryable
            .OrderBy(b => b.StartTime)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return bookings.Select(BookingMapper.ToResponse).ToList();
    }
}
#pragma warning restore CA1812
