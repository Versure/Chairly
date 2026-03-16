using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Results;
using Chairly.Api.Shared.Tenancy;
using Chairly.Domain.Entities;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;

#pragma warning disable CA1812
namespace Chairly.Api.Features.Clients.CreateRecipe;

internal sealed class CreateRecipeHandler(ChairlyDbContext db, ITenantContext tenantContext) : IRequestHandler<CreateRecipeCommand, OneOf<RecipeResponse, NotFound, Unprocessable, Conflict, Forbidden>>
{
    public async Task<OneOf<RecipeResponse, NotFound, Unprocessable, Conflict, Forbidden>> Handle(CreateRecipeCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var booking = await db.Bookings
            .FirstOrDefaultAsync(b => b.Id == command.BookingId && b.TenantId == tenantContext.TenantId, cancellationToken)
            .ConfigureAwait(false);

        if (booking is null)
        {
            return new NotFound();
        }

        if (booking.CompletedAtUtc is null)
        {
            return new Unprocessable("Booking is niet afgerond");
        }

        var recipeExists = await db.Recipes
            .AnyAsync(r => r.BookingId == command.BookingId && r.TenantId == tenantContext.TenantId, cancellationToken)
            .ConfigureAwait(false);

        if (recipeExists)
        {
            return new Conflict("Er bestaat al een recept voor deze boeking");
        }

        // Authorization check will be added with Keycloak integration
        // StaffMember can only create for own bookings

        var recipe = new Recipe
        {
            Id = Guid.NewGuid(),
            TenantId = tenantContext.TenantId,
            BookingId = booking.Id,
            ClientId = booking.ClientId,
            StaffMemberId = booking.StaffMemberId,
            Title = command.Title,
            Notes = command.Notes,
            Products = command.Products.Select(p => new RecipeProduct
            {
                Id = Guid.NewGuid(),
                Name = p.Name,
                Brand = p.Brand,
                Quantity = p.Quantity,
                SortOrder = p.SortOrder,
            }).ToList(),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedBy = tenantContext.UserId,
        };

        db.Recipes.Add(recipe);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return RecipeMapper.ToResponse(recipe);
    }
}
#pragma warning restore CA1812
