using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Results;
using Chairly.Api.Shared.Tenancy;
using Chairly.Domain.Entities;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;

namespace Chairly.Api.Features.Bookings.UpdateBooking;

#pragma warning disable CA1812
internal sealed class UpdateBookingHandler(ChairlyDbContext db) : IRequestHandler<UpdateBookingCommand, OneOf<BookingResponse, NotFound, Conflict>>
{
    public async Task<OneOf<BookingResponse, NotFound, Conflict>> Handle(UpdateBookingCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var booking = await db.Bookings
            .Include(b => b.BookingServices)
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

        if (command.ServiceIds.Count == 0)
        {
            throw new ValidationException(new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                { "ServiceIds", ["At least one service is required."] },
            });
        }

        var notFoundResult = await ValidateEntitiesAsync(command, cancellationToken).ConfigureAwait(false);
        if (notFoundResult.HasValue)
        {
            return notFoundResult.Value;
        }

        var services = await db.Services
            .Where(s => command.ServiceIds.Contains(s.Id) && s.TenantId == TenantConstants.DefaultTenantId && s.IsActive)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (services.Count != command.ServiceIds.Count)
        {
            return new NotFound();
        }

        var serviceMap = services.ToDictionary(s => s.Id);
        var newEndTime = command.StartTime + command.ServiceIds.Aggregate(TimeSpan.Zero, (acc, id) => acc + serviceMap[id].Duration);

        if (await HasOverlapAsync(command.StaffMemberId, command.StartTime, newEndTime, command.Id, cancellationToken).ConfigureAwait(false))
        {
            return new Conflict();
        }

        ApplyUpdate(booking, command, newEndTime, serviceMap);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return BookingMapper.ToResponse(booking);
    }

    private void ApplyUpdate(Booking booking, UpdateBookingCommand command, DateTimeOffset newEndTime, Dictionary<Guid, Service> serviceMap)
    {
        booking.ClientId = command.ClientId;
        booking.StaffMemberId = command.StaffMemberId;
        booking.StartTime = command.StartTime;
        booking.EndTime = newEndTime;
        booking.Notes = command.Notes;
        booking.UpdatedAtUtc = DateTimeOffset.UtcNow;
#pragma warning disable MA0026 // TODO: Replace with authenticated user ID from Keycloak
        booking.UpdatedBy = Guid.Empty;
#pragma warning restore MA0026

        db.RemoveRange(booking.BookingServices);
        booking.BookingServices.Clear();
        foreach (var (serviceId, index) in command.ServiceIds.Select((id, i) => (id, i)))
        {
            booking.BookingServices.Add(new BookingService
            {
                Id = Guid.NewGuid(),
                ServiceId = serviceId,
                ServiceName = serviceMap[serviceId].Name,
                Duration = serviceMap[serviceId].Duration,
                Price = serviceMap[serviceId].Price,
                SortOrder = index,
            });
        }
    }

    private async Task<NotFound?> ValidateEntitiesAsync(UpdateBookingCommand command, CancellationToken cancellationToken)
    {
        var client = await db.Clients
            .FirstOrDefaultAsync(c => c.Id == command.ClientId && c.TenantId == TenantConstants.DefaultTenantId, cancellationToken)
            .ConfigureAwait(false);

        if (client is null || client.DeletedAtUtc != null)
        {
            return new NotFound();
        }

        var staffMember = await db.StaffMembers
            .FirstOrDefaultAsync(s => s.Id == command.StaffMemberId && s.TenantId == TenantConstants.DefaultTenantId, cancellationToken)
            .ConfigureAwait(false);

        if (staffMember is null || staffMember.DeactivatedAtUtc != null)
        {
            return new NotFound();
        }

        return null;
    }

    private async Task<bool> HasOverlapAsync(Guid staffMemberId, DateTimeOffset startTime, DateTimeOffset endTime, Guid excludeBookingId, CancellationToken cancellationToken)
    {
        return await db.Bookings
            .AnyAsync(
                b => b.TenantId == TenantConstants.DefaultTenantId
                    && b.StaffMemberId == staffMemberId
                    && b.Id != excludeBookingId
                    && b.CancelledAtUtc == null
                    && b.NoShowAtUtc == null
                    && b.StartTime < endTime
                    && b.EndTime > startTime,
                cancellationToken)
            .ConfigureAwait(false);
    }

}
#pragma warning restore CA1812
