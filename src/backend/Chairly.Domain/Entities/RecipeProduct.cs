namespace Chairly.Domain.Entities;

public class RecipeProduct
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Brand { get; set; }
    public string? Quantity { get; set; }
    public int SortOrder { get; set; }
}
