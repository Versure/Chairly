using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Results;
using Chairly.Api.Shared.Tenancy;
using Chairly.Domain.Entities;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;

namespace Chairly.Api.Features.Bookings.CreateBooking;

#pragma warning disable CA1812
internal sealed class CreateBookingHandler(ChairlyDbContext db) : IRequestHandler<CreateBookingCommand, OneOf<BookingResponse, NotFound, Conflict>>
{
    public async Task<OneOf<BookingResponse, NotFound, Conflict>> Handle(CreateBookingCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.ServiceIds.Count == 0)
        {
            throw new ValidationException(new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                { "ServiceIds", ["At least one service is required."] },
            });
        }

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

        var services = await db.Services
            .Where(s => command.ServiceIds.Contains(s.Id) && s.TenantId == TenantConstants.DefaultTenantId && s.IsActive)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (services.Count != command.ServiceIds.Count)
        {
            return new NotFound();
        }

        var serviceMap = services.ToDictionary(s => s.Id);
        var endTime = command.StartTime + command.ServiceIds.Aggregate(TimeSpan.Zero, (acc, id) => acc + serviceMap[id].Duration);

        if (await HasOverlapAsync(command.StaffMemberId, command.StartTime, endTime, cancellationToken).ConfigureAwait(false))
        {
            return new Conflict();
        }

        var booking = CreateBooking(command, endTime, serviceMap);

        db.Bookings.Add(booking);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return BookingMapper.ToResponse(booking);
    }

    private async Task<bool> HasOverlapAsync(Guid staffMemberId, DateTimeOffset startTime, DateTimeOffset endTime, CancellationToken cancellationToken)
    {
        return await db.Bookings
            .AnyAsync(
                b => b.TenantId == TenantConstants.DefaultTenantId
                    && b.StaffMemberId == staffMemberId
                    && b.CancelledAtUtc == null
                    && b.NoShowAtUtc == null
                    && b.StartTime < endTime
                    && b.EndTime > startTime,
                cancellationToken)
            .ConfigureAwait(false);
    }

    private static Booking CreateBooking(CreateBookingCommand command, DateTimeOffset endTime, Dictionary<Guid, Service> serviceMap)
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
#pragma warning disable MA0026 // TODO: Replace with authenticated user ID from Keycloak
            CreatedBy = Guid.Empty,
#pragma warning restore MA0026
            BookingServices = command.ServiceIds
                .Select((serviceId, index) => new BookingService
                {
                    Id = Guid.NewGuid(),
                    ServiceId = serviceId,
                    ServiceName = serviceMap[serviceId].Name,
                    Duration = serviceMap[serviceId].Duration,
                    Price = serviceMap[serviceId].Price,
                    SortOrder = index,
                })
                .ToList(),
        };
    }
}
#pragma warning restore CA1812
