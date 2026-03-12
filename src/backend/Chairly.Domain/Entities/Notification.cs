using Chairly.Domain.Enums;

namespace Chairly.Domain.Entities;

public class Notification
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid RecipientId { get; set; }
    public RecipientType RecipientType { get; set; }
    public NotificationChannel Channel { get; set; }
    public NotificationType Type { get; set; }
    public Guid ReferenceId { get; set; }
    public DateTimeOffset ScheduledAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid? UpdatedBy { get; set; }
    public DateTimeOffset? SentAtUtc { get; set; }
    public DateTimeOffset? FailedAtUtc { get; set; }
    public string? FailureReason { get; set; }
    public int RetryCount { get; set; }
}
