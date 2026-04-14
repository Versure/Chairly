namespace Chairly.Domain.Events;

public record NewsletterDeliveryRequestedEvent(Guid TenantId, Guid CampaignId, Guid DeliveryId);
