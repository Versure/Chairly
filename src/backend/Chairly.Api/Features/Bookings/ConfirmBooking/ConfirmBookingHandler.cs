using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Results;
using Chairly.Api.Shared.Tenancy;
using Chairly.Domain.Events;
using Chairly.Infrastructure.Messaging;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;

namespace Chairly.Api.Features.Bookings.ConfirmBooking;

#pragma warning disable CA1812
internal sealed class ConfirmBookingHandler(ChairlyDbContext db, IBookingEventPublisher eventPublisher) : IRequestHandler<ConfirmBookingCommand, OneOf<Success, NotFound, Conflict>>
{
    public async Task<OneOf<Success, NotFound, Conflict>> Handle(ConfirmBookingCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var booking = await db.Bookings
            .FirstOrDefaultAsync(b => b.Id == command.Id && b.TenantId == TenantConstants.DefaultTenantId, cancellationToken)
            .ConfigureAwait(false);

        if (booking is null)
        {
            return new NotFound();
        }

        if (booking.ConfirmedAtUtc != null || booking.StartedAtUtc != null || booking.CompletedAtUtc != null || booking.CancelledAtUtc != null || booking.NoShowAtUtc != null)
        {
            return new Conflict();
        }

        booking.ConfirmedAtUtc = DateTimeOffset.UtcNow;
#pragma warning disable MA0026 // TODO: Replace with authenticated user ID from Keycloak (see Keycloak integration)
        booking.ConfirmedBy = Guid.Empty;
#pragma warning restore MA0026

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await eventPublisher.PublishConfirmedAsync(
            new BookingConfirmedEvent(booking.TenantId, booking.Id, booking.ClientId, booking.StartTime),
            cancellationToken).ConfigureAwait(false);

        return new Success();
    }
}
#pragma warning restore CA1812
