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

        if (!await ValidateReferencesAsync(command, cancellationToken).ConfigureAwait(false))
        {
            return new NotFound();
        }

        var services = await LoadActiveServicesAsync(command.ServiceIds, cancellationToken).ConfigureAwait(false);
        if (services.Count != command.ServiceIds.Count)
        {
            return new NotFound();
        }

        var totalDuration = services.Aggregate(TimeSpan.Zero, (sum, s) => sum + s.Duration);
        var newEndTime = command.StartTime + totalDuration;

        if (await HasOverlapAsync(command.Id, command.StaffMemberId, command.StartTime, newEndTime, cancellationToken).ConfigureAwait(false))
        {
            return new Conflict();
        }

        ApplyUpdates(booking, command, newEndTime);
        ReplaceBookingServices(booking, command.ServiceIds, services);

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // Reload booking services after save to get the newly added services in the navigation property
        await db.Entry(booking).Collection(b => b.BookingServices).LoadAsync(cancellationToken).ConfigureAwait(false);

        return BookingMapper.ToResponse(booking);
    }

    private async Task<bool> ValidateReferencesAsync(UpdateBookingCommand command, CancellationToken cancellationToken)
    {
        var clientExists = await db.Clients
            .AnyAsync(c => c.Id == command.ClientId && c.TenantId == TenantConstants.DefaultTenantId && c.DeletedAtUtc == null, cancellationToken)
            .ConfigureAwait(false);

        if (!clientExists)
        {
            return false;
        }

        return await db.StaffMembers
            .AnyAsync(s => s.Id == command.StaffMemberId && s.TenantId == TenantConstants.DefaultTenantId && s.DeactivatedAtUtc == null, cancellationToken)
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

    private static void ApplyUpdates(Booking booking, UpdateBookingCommand command, DateTimeOffset newEndTime)
    {
        booking.ClientId = command.ClientId;
        booking.StaffMemberId = command.StaffMemberId;
        booking.StartTime = command.StartTime;
        booking.EndTime = newEndTime;
        booking.Notes = command.Notes;
        booking.UpdatedAtUtc = DateTimeOffset.UtcNow;
#pragma warning disable MA0026 // TODO: Replace with authenticated user ID from Keycloak (see Keycloak integration)
        booking.UpdatedBy = Guid.Empty;
#pragma warning restore MA0026
    }

    private void ReplaceBookingServices(Booking booking, List<Guid> serviceIds, List<Service> services)
    {
        db.BookingServices.RemoveRange(booking.BookingServices);
        booking.BookingServices.Clear();

        var serviceMap = services.ToDictionary(s => s.Id);
        for (var i = 0; i < serviceIds.Count; i++)
        {
            var service = serviceMap[serviceIds[i]];
            var bookingService = new BookingService
            {
                Id = Guid.NewGuid(),
                BookingId = booking.Id,
                ServiceId = service.Id,
                ServiceName = service.Name,
                Duration = service.Duration,
                Price = service.Price,
                SortOrder = i,
            };

            // Use DbSet.Add to ensure the entity is tracked as Added (avoids InMemory provider
            // DbUpdateConcurrencyException when replacing child entities on a tracked parent).
            db.BookingServices.Add(bookingService);
        }
    }
}
#pragma warning restore CA1812
