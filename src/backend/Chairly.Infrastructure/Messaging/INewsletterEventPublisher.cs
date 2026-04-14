using Chairly.Domain.Events;

namespace Chairly.Infrastructure.Messaging;

public interface INewsletterEventPublisher
{
    Task PublishCampaignQueuedAsync(NewsletterCampaignQueuedEvent domainEvent, CancellationToken cancellationToken = default);
    Task PublishDeliveryRequestedAsync(NewsletterDeliveryRequestedEvent domainEvent, CancellationToken cancellationToken = default);
    Task PublishTestRequestedAsync(NewsletterTestRequestedEvent domainEvent, CancellationToken cancellationToken = default);
}
