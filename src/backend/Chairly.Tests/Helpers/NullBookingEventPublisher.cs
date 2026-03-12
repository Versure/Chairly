using Chairly.Domain.Events;
using Chairly.Infrastructure.Messaging;

namespace Chairly.Tests.Helpers;

internal sealed class NullBookingEventPublisher : IBookingEventPublisher
{
    public Task PublishCreatedAsync(BookingCreatedEvent bookingEvent, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task PublishConfirmedAsync(BookingConfirmedEvent bookingEvent, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task PublishCancelledAsync(BookingCancelledEvent bookingEvent, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
