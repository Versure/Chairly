namespace Chairly.Domain.Entities;

public class NewsletterDelivery
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid CampaignId { get; set; }
    public Guid ClientId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string UnsubscribeToken { get; set; } = string.Empty;

    public DateTimeOffset? SentAtUtc { get; set; }
    public DateTimeOffset? FailedAtUtc { get; set; }
    public string? FailureReason { get; set; }
    public DateTimeOffset? UnsubscribedAtUtc { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid CreatedBy { get; set; }
}
