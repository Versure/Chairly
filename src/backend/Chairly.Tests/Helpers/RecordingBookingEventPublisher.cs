using Chairly.Domain.Events;
using Chairly.Infrastructure.Messaging;

namespace Chairly.Tests.Helpers;

internal sealed class RecordingBookingEventPublisher : IBookingEventPublisher
{
    public List<BookingCreatedEvent> CreatedEvents { get; } = [];
    public List<BookingConfirmedEvent> ConfirmedEvents { get; } = [];
    public List<BookingCancelledEvent> CancelledEvents { get; } = [];

    public Task PublishCreatedAsync(BookingCreatedEvent bookingEvent, CancellationToken cancellationToken = default)
    {
        CreatedEvents.Add(bookingEvent);
        return Task.CompletedTask;
    }

    public Task PublishConfirmedAsync(BookingConfirmedEvent bookingEvent, CancellationToken cancellationToken = default)
    {
        ConfirmedEvents.Add(bookingEvent);
        return Task.CompletedTask;
    }

    public Task PublishCancelledAsync(BookingCancelledEvent bookingEvent, CancellationToken cancellationToken = default)
    {
        CancelledEvents.Add(bookingEvent);
        return Task.CompletedTask;
    }
}
