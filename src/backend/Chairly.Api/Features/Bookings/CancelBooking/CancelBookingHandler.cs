using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Results;
using Chairly.Api.Shared.Tenancy;
using Chairly.Domain.Events;
using Chairly.Infrastructure.Messaging;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OneOf;
using OneOf.Types;

namespace Chairly.Api.Features.Bookings.CancelBooking;

#pragma warning disable CA1812
internal sealed partial class CancelBookingHandler(ChairlyDbContext db, IBookingEventPublisher eventPublisher, ILogger<CancelBookingHandler> logger, ITenantContext tenantContext) : IRequestHandler<CancelBookingCommand, OneOf<Success, NotFound, Conflict>>
{
    public async Task<OneOf<Success, NotFound, Conflict>> Handle(CancelBookingCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var booking = await db.Bookings
            .FirstOrDefaultAsync(b => b.Id == command.Id && b.TenantId == tenantContext.TenantId, cancellationToken)
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
        booking.CancelledBy = tenantContext.UserId;

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            await eventPublisher.PublishCancelledAsync(
                new BookingCancelledEvent(booking.TenantId, booking.Id, booking.ClientId),
                cancellationToken).ConfigureAwait(false);
        }
#pragma warning disable CA1031 // Best-effort event publishing; booking is already persisted
        catch (Exception ex)
#pragma warning restore CA1031
        {
            LogEventPublishFailed(logger, booking.Id, ex);
        }

        return new Success();
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to publish event for booking {BookingId}; notification may be delayed")]
    private static partial void LogEventPublishFailed(ILogger logger, Guid bookingId, Exception exception);
}
#pragma warning restore CA1812
