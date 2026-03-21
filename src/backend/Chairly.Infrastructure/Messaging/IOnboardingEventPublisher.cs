using Chairly.Domain.Events;

namespace Chairly.Infrastructure.Messaging;

public interface IOnboardingEventPublisher
{
    Task PublishSubscriptionCreatedAsync(SubscriptionCreatedEvent domainEvent, CancellationToken cancellationToken = default);
}
