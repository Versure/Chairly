using Chairly.Domain.Enums;

namespace Chairly.Domain.Entities;

public class NewsletterCampaign
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string BodyHtml { get; set; } = string.Empty;
    public NewsletterRecipientFilter RecipientFilter { get; set; } = NewsletterRecipientFilter.AllSubscribed;

    public DateTimeOffset? ScheduledAtUtc { get; set; }
    public Guid? ScheduledBy { get; set; }
    public DateTimeOffset? QueuedAtUtc { get; set; }
    public Guid? QueuedBy { get; set; }
    public DateTimeOffset? SentAtUtc { get; set; }
    public Guid? SentBy { get; set; }
    public DateTimeOffset? CancelledAtUtc { get; set; }
    public Guid? CancelledBy { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid? UpdatedBy { get; set; }

#pragma warning disable CA1002, MA0016, CA2227 // Mutable navigation collection required by EF Core
    public List<NewsletterDelivery> Deliveries { get; set; } = [];
#pragma warning restore CA1002, MA0016, CA2227
}
