namespace Chairly.Domain.Events;

public record BookingConfirmedEvent(Guid TenantId, Guid BookingId, Guid ClientId, DateTimeOffset StartTime);
