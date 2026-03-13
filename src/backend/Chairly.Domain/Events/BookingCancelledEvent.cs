namespace Chairly.Domain.Events;

public record BookingCancelledEvent(Guid TenantId, Guid BookingId, Guid ClientId);
