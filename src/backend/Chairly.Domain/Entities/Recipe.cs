namespace Chairly.Domain.Entities;

public class Recipe
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid BookingId { get; set; }
    public Guid ClientId { get; set; }
    public Guid StaffMemberId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Notes { get; set; }

#pragma warning disable CA1002, CA2227, MA0016 // EF Core requires mutable collection for navigation property
    public List<RecipeProduct> Products { get; set; } = [];
#pragma warning restore CA1002, CA2227, MA0016

    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid? UpdatedBy { get; set; }
}
