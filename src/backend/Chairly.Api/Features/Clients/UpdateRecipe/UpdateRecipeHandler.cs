using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Results;
using Chairly.Api.Shared.Tenancy;
using Chairly.Domain.Entities;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;

#pragma warning disable CA1812
namespace Chairly.Api.Features.Clients.UpdateRecipe;

internal sealed class UpdateRecipeHandler(ChairlyDbContext db) : IRequestHandler<UpdateRecipeCommand, OneOf<RecipeResponse, NotFound, Forbidden>>
{
    public async Task<OneOf<RecipeResponse, NotFound, Forbidden>> Handle(UpdateRecipeCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var recipe = await db.Recipes
            .Include(r => r.Products)
            .FirstOrDefaultAsync(r => r.Id == command.Id && r.TenantId == TenantConstants.DefaultTenantId, cancellationToken)
            .ConfigureAwait(false);

        if (recipe is null)
        {
            return new NotFound();
        }

        // Authorization check will be added with Keycloak integration
        // StaffMember can only edit own recipes

        recipe.Title = command.Title;
        recipe.Notes = command.Notes;
        recipe.UpdatedAtUtc = DateTimeOffset.UtcNow;
#pragma warning disable MA0026 // Replace with authenticated user ID from Keycloak
        recipe.UpdatedBy = Guid.Empty;
#pragma warning restore MA0026

        // Full replace of owned products collection
        recipe.Products.Clear();

        foreach (var p in command.Products)
        {
            recipe.Products.Add(new RecipeProduct
            {
                Id = Guid.NewGuid(),
                Name = p.Name,
                Brand = p.Brand,
                Quantity = p.Quantity,
                SortOrder = p.SortOrder,
            });
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return RecipeMapper.ToResponse(recipe);
    }
}
#pragma warning restore CA1812
