using Chairly.Domain.Events;
using Chairly.Infrastructure.Messaging;

namespace Chairly.Tests.Helpers;

internal sealed class NullNewsletterEventPublisher : INewsletterEventPublisher
{
    public Task PublishCampaignQueuedAsync(NewsletterCampaignQueuedEvent domainEvent, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task PublishDeliveryRequestedAsync(NewsletterDeliveryRequestedEvent domainEvent, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task PublishTestRequestedAsync(NewsletterTestRequestedEvent domainEvent, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
