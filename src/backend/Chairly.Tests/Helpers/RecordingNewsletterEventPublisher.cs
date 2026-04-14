using Chairly.Domain.Events;
using Chairly.Infrastructure.Messaging;

namespace Chairly.Tests.Helpers;

internal sealed class RecordingNewsletterEventPublisher : INewsletterEventPublisher
{
    public List<NewsletterCampaignQueuedEvent> CampaignQueuedEvents { get; } = [];
    public List<NewsletterDeliveryRequestedEvent> DeliveryRequestedEvents { get; } = [];
    public List<NewsletterTestRequestedEvent> TestRequestedEvents { get; } = [];

    public Task PublishCampaignQueuedAsync(NewsletterCampaignQueuedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        CampaignQueuedEvents.Add(domainEvent);
        return Task.CompletedTask;
    }

    public Task PublishDeliveryRequestedAsync(NewsletterDeliveryRequestedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        DeliveryRequestedEvents.Add(domainEvent);
        return Task.CompletedTask;
    }

    public Task PublishTestRequestedAsync(NewsletterTestRequestedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        TestRequestedEvents.Add(domainEvent);
        return Task.CompletedTask;
    }
}
