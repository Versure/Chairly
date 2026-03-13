using Chairly.Domain.Events;

namespace Chairly.Infrastructure.Messaging;

public interface IBookingEventPublisher
{
    Task PublishCreatedAsync(BookingCreatedEvent bookingEvent, CancellationToken cancellationToken = default);
    Task PublishConfirmedAsync(BookingConfirmedEvent bookingEvent, CancellationToken cancellationToken = default);
    Task PublishCancelledAsync(BookingCancelledEvent bookingEvent, CancellationToken cancellationToken = default);
}
