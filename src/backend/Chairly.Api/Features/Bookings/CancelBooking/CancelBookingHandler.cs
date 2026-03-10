using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Results;
using Chairly.Api.Shared.Tenancy;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;

namespace Chairly.Api.Features.Bookings.CancelBooking;

#pragma warning disable CA1812
internal sealed class CancelBookingHandler(ChairlyDbContext db) : IRequestHandler<CancelBookingCommand, OneOf<Success, NotFound, Conflict>>
{
    public async Task<OneOf<Success, NotFound, Conflict>> Handle(CancelBookingCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var booking = await db.Bookings
            .FirstOrDefaultAsync(b => b.Id == command.Id && b.TenantId == TenantConstants.DefaultTenantId, cancellationToken)
            .ConfigureAwait(false);

        if (booking is null)
        {
            return new NotFound();
        }

        if (booking.CompletedAtUtc != null || booking.CancelledAtUtc != null || booking.NoShowAtUtc != null)
        {
            return new Conflict();
        }

        booking.CancelledAtUtc = DateTimeOffset.UtcNow;
#pragma warning disable MA0026 // TODO: Replace with authenticated user ID from Keycloak (see Keycloak integration)
        booking.CancelledBy = Guid.Empty;
#pragma warning restore MA0026

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new Success();
    }
}
#pragma warning restore CA1812
