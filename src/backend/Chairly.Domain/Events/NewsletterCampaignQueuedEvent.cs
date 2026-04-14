namespace Chairly.Domain.Events;

public record NewsletterCampaignQueuedEvent(Guid TenantId, Guid CampaignId, DateTimeOffset QueuedAtUtc);
