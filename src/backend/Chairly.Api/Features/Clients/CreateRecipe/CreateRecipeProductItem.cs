using System.ComponentModel.DataAnnotations;

#pragma warning disable CA1812 // Instantiated via ASP.NET Core model binding
namespace Chairly.Api.Features.Clients.CreateRecipe;

internal sealed class CreateRecipeProductItem
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Brand { get; set; }

    [MaxLength(50)]
    public string? Quantity { get; set; }

    public int SortOrder { get; set; }
}
#pragma warning restore CA1812
