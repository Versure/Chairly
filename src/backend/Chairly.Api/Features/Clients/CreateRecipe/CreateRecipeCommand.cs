using System.ComponentModel.DataAnnotations;
using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Results;
using OneOf;
using OneOf.Types;

#pragma warning disable CA1812 // Instantiated via ASP.NET Core model binding
namespace Chairly.Api.Features.Clients.CreateRecipe;

internal sealed class CreateRecipeCommand : IRequest<OneOf<RecipeResponse, NotFound, Unprocessable, Conflict, Forbidden>>
{
    [Required]
    public Guid BookingId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Notes { get; set; }

#pragma warning disable CA1002, CA2227, MA0016 // Mutable list needed for model binding
    public List<CreateRecipeProductItem> Products { get; set; } = [];
#pragma warning restore CA1002, CA2227, MA0016
}
#pragma warning restore CA1812
