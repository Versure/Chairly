using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Results;
using Chairly.Api.Shared.Tenancy;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;

#pragma warning disable CA1812
namespace Chairly.Api.Features.Clients.GetRecipeByBooking;

internal sealed class GetRecipeByBookingHandler(ChairlyDbContext db, ITenantContext tenantContext) : IRequestHandler<GetRecipeByBookingQuery, OneOf<RecipeResponse, NotFound, Forbidden>>
{
    public async Task<OneOf<RecipeResponse, NotFound, Forbidden>> Handle(GetRecipeByBookingQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var recipe = await db.Recipes
            .Include(r => r.Products)
            .FirstOrDefaultAsync(r => r.BookingId == query.BookingId && r.TenantId == tenantContext.TenantId, cancellationToken)
            .ConfigureAwait(false);

        if (recipe is null)
        {
            return new NotFound();
        }

        // Authorization check will be added with Keycloak integration
        // StaffMember can only view own recipes

        return RecipeMapper.ToResponse(recipe);
    }
}
#pragma warning restore CA1812
