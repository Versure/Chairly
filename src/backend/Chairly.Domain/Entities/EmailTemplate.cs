using Chairly.Domain.Enums;

namespace Chairly.Domain.Entities;

public class EmailTemplate
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public NotificationType TemplateType { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid? UpdatedBy { get; set; }
}
