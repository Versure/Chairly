using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Results;
using Chairly.Api.Shared.Tenancy;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;

namespace Chairly.Api.Features.Bookings.MarkBookingNoShow;

#pragma warning disable CA1812
internal sealed class MarkBookingNoShowHandler(ChairlyDbContext db) : IRequestHandler<MarkBookingNoShowCommand, OneOf<Success, NotFound, Conflict>>
{
    public async Task<OneOf<Success, NotFound, Conflict>> Handle(MarkBookingNoShowCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var booking = await db.Bookings
            .FirstOrDefaultAsync(b => b.Id == command.Id && b.TenantId == TenantConstants.DefaultTenantId, cancellationToken)
            .ConfigureAwait(false);

        if (booking is null)
        {
            return new NotFound();
        }

        if (booking.StartedAtUtc != null || booking.CompletedAtUtc != null || booking.CancelledAtUtc != null || booking.NoShowAtUtc != null)
        {
            return new Conflict();
        }

        booking.NoShowAtUtc = DateTimeOffset.UtcNow;
#pragma warning disable MA0026 // TODO: Replace with authenticated user ID from Keycloak (see Keycloak integration)
        booking.NoShowBy = Guid.Empty;
#pragma warning restore MA0026

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new Success();
    }
}
#pragma warning restore CA1812
