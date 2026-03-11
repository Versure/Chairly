namespace Chairly.Domain.Entities;

public class Service
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TimeSpan Duration { get; set; }
    public decimal Price { get; set; }
    public decimal? VatRate { get; set; }
    public Guid? CategoryId { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid? UpdatedBy { get; set; }

    public ServiceCategory? Category { get; set; }
}
