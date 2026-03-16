using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Tenancy;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;

namespace Chairly.Api.Features.Bookings.GetBooking;

#pragma warning disable CA1812
internal sealed class GetBookingHandler(ChairlyDbContext db, ITenantContext tenantContext) : IRequestHandler<GetBookingQuery, OneOf<BookingResponse, NotFound>>
{
    public async Task<OneOf<BookingResponse, NotFound>> Handle(GetBookingQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var booking = await db.Bookings
            .Include(b => b.BookingServices)
            .FirstOrDefaultAsync(b => b.Id == query.Id && b.TenantId == tenantContext.TenantId, cancellationToken)
            .ConfigureAwait(false);

        if (booking is null)
        {
            return new NotFound();
        }

        return BookingMapper.ToResponse(booking);
    }
}
#pragma warning restore CA1812
