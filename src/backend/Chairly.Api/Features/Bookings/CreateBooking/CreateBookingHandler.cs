using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Results;
using Chairly.Api.Shared.Tenancy;
using Chairly.Domain.Entities;
using Chairly.Domain.Events;
using Chairly.Infrastructure.Messaging;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OneOf;
using OneOf.Types;

namespace Chairly.Api.Features.Bookings.CreateBooking;

#pragma warning disable CA1812
internal sealed partial class CreateBookingHandler(ChairlyDbContext db, IBookingEventPublisher eventPublisher, ILogger<CreateBookingHandler> logger) : IRequestHandler<CreateBookingCommand, OneOf<BookingResponse, NotFound, Conflict>>
{
    public async Task<OneOf<BookingResponse, NotFound, Conflict>> Handle(CreateBookingCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!await ClientExistsAsync(command.ClientId, cancellationToken).ConfigureAwait(false))
        {
            return new NotFound();
        }

        if (!await StaffMemberExistsAsync(command.StaffMemberId, cancellationToken).ConfigureAwait(false))
        {
            return new NotFound();
        }

        var services = await LoadActiveServicesAsync(command.ServiceIds, cancellationToken).ConfigureAwait(false);
        if (services.Count != command.ServiceIds.Count)
        {
            return new NotFound();
        }

        var totalDuration = services.Aggregate(TimeSpan.Zero, (sum, s) => sum + s.Duration);
        var endTime = command.StartTime + totalDuration;

        if (await HasOverlapAsync(Guid.Empty, command.StaffMemberId, command.StartTime, endTime, cancellationToken).ConfigureAwait(false))
        {
            return new Conflict();
        }

        var booking = BuildBooking(command, endTime);
        AddBookingServices(booking, command.ServiceIds, services);

        db.Bookings.Add(booking);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            await eventPublisher.PublishCreatedAsync(
                new BookingCreatedEvent(booking.TenantId, booking.Id, booking.ClientId, booking.StartTime),
                cancellationToken).ConfigureAwait(false);
        }
#pragma warning disable CA1031 // Best-effort event publishing; booking is already persisted
        catch (Exception ex)
#pragma warning restore CA1031
        {
            LogEventPublishFailed(logger, booking.Id, ex);
        }

        return BookingMapper.ToResponse(booking);
    }

    private async Task<bool> ClientExistsAsync(Guid clientId, CancellationToken cancellationToken)
    {
        return await db.Clients
            .AnyAsync(c => c.Id == clientId && c.TenantId == TenantConstants.DefaultTenantId && c.DeletedAtUtc == null, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<bool> StaffMemberExistsAsync(Guid staffMemberId, CancellationToken cancellationToken)
    {
        return await db.StaffMembers
            .AnyAsync(s => s.Id == staffMemberId && s.TenantId == TenantConstants.DefaultTenantId && s.DeactivatedAtUtc == null, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<List<Service>> LoadActiveServicesAsync(List<Guid> serviceIds, CancellationToken cancellationToken)
    {
        return await db.Services
            .Where(s => serviceIds.Contains(s.Id) && s.TenantId == TenantConstants.DefaultTenantId && s.IsActive)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<bool> HasOverlapAsync(Guid excludeBookingId, Guid staffMemberId, DateTimeOffset startTime, DateTimeOffset endTime, CancellationToken cancellationToken)
    {
        return await db.Bookings
            .AnyAsync(b =>
                b.Id != excludeBookingId
                && b.StaffMemberId == staffMemberId
                && b.TenantId == TenantConstants.DefaultTenantId
                && b.CancelledAtUtc == null
                && b.NoShowAtUtc == null
                && b.StartTime < endTime
                && b.EndTime > startTime, cancellationToken)
            .ConfigureAwait(false);
    }

    private static Booking BuildBooking(CreateBookingCommand command, DateTimeOffset endTime)
    {
        return new Booking
        {
            Id = Guid.NewGuid(),
            TenantId = TenantConstants.DefaultTenantId,
            ClientId = command.ClientId,
            StaffMemberId = command.StaffMemberId,
            StartTime = command.StartTime,
            EndTime = endTime,
            Notes = command.Notes,
            CreatedAtUtc = DateTimeOffset.UtcNow,
#pragma warning disable MA0026 // TODO: Replace with authenticated user ID from Keycloak (see Keycloak integration)
            CreatedBy = Guid.Empty,
#pragma warning restore MA0026
        };
    }

    private static void AddBookingServices(Booking booking, List<Guid> serviceIds, List<Service> services)
    {
        var serviceMap = services.ToDictionary(s => s.Id);
        for (var i = 0; i < serviceIds.Count; i++)
        {
            var service = serviceMap[serviceIds[i]];
            booking.BookingServices.Add(new BookingService
            {
                Id = Guid.NewGuid(),
                BookingId = booking.Id,
                ServiceId = service.Id,
                ServiceName = service.Name,
                Duration = service.Duration,
                Price = service.Price,
                SortOrder = i,
            });
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to publish event for booking {BookingId}; notification may be delayed")]
    private static partial void LogEventPublishFailed(ILogger logger, Guid bookingId, Exception exception);
}
#pragma warning restore CA1812
