namespace Chairly.Domain.Events;

public record BookingCreatedEvent(Guid TenantId, Guid BookingId, Guid ClientId, DateTimeOffset StartTime);
