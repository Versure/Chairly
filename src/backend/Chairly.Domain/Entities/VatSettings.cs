namespace Chairly.Domain.Entities;

public class VatSettings
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public decimal DefaultVatRate { get; set; } = 21m;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid? UpdatedBy { get; set; }
}
