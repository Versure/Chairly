using Chairly.Domain.Entities;

namespace Chairly.Api.Features.Clients;

internal static class RecipeMapper
{
    public static RecipeResponse ToResponse(Recipe recipe)
    {
        ArgumentNullException.ThrowIfNull(recipe);

        return new RecipeResponse(
            recipe.Id,
            recipe.BookingId,
            recipe.ClientId,
            recipe.StaffMemberId,
            recipe.Title,
            recipe.Notes,
            recipe.Products.OrderBy(p => p.SortOrder).Select(p => new RecipeProductResponse(
                p.Id,
                p.Name,
                p.Brand,
                p.Quantity,
                p.SortOrder)).ToList(),
            recipe.CreatedAtUtc,
            recipe.CreatedBy,
            recipe.UpdatedAtUtc,
            recipe.UpdatedBy);
    }
}
