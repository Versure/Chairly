namespace Chairly.Domain.Events;

public record NewsletterTestRequestedEvent(Guid TenantId, Guid CampaignId, string RecipientEmail, Guid RequestedBy);
