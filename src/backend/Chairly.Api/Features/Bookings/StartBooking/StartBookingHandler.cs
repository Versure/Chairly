using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Results;
using Chairly.Api.Shared.Tenancy;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;

namespace Chairly.Api.Features.Bookings.StartBooking;

#pragma warning disable CA1812
internal sealed class StartBookingHandler(ChairlyDbContext db, ITenantContext tenantContext) : IRequestHandler<StartBookingCommand, OneOf<Success, NotFound, Conflict>>
{
    public async Task<OneOf<Success, NotFound, Conflict>> Handle(StartBookingCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var booking = await db.Bookings
            .FirstOrDefaultAsync(b => b.Id == command.Id && b.TenantId == tenantContext.TenantId, cancellationToken)
            .ConfigureAwait(false);

        if (booking is null)
        {
            return new NotFound();
        }

        if (booking.StartedAtUtc != null || booking.CompletedAtUtc != null || booking.CancelledAtUtc != null || booking.NoShowAtUtc != null)
        {
            return new Conflict();
        }

        booking.StartedAtUtc = DateTimeOffset.UtcNow;
        booking.StartedBy = tenantContext.UserId;

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new Success();
    }
}
#pragma warning restore CA1812
