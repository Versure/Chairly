using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Tenancy;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;

#pragma warning disable CA1812
namespace Chairly.Api.Features.Clients.GetClientRecipes;

internal sealed class GetClientRecipesHandler(ChairlyDbContext db) : IRequestHandler<GetClientRecipesQuery, OneOf<IReadOnlyList<ClientRecipeSummaryResponse>, NotFound>>
{
    public async Task<OneOf<IReadOnlyList<ClientRecipeSummaryResponse>, NotFound>> Handle(GetClientRecipesQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var clientExists = await db.Clients
            .AnyAsync(c => c.Id == query.ClientId && c.TenantId == TenantConstants.DefaultTenantId, cancellationToken)
            .ConfigureAwait(false);

        if (!clientExists)
        {
            return new NotFound();
        }

        // Authorization filter will be added with Keycloak integration
        // StaffMember: .Where(r => r.StaffMemberId == currentUserId)

        var recipes = await db.Recipes
            .Include(r => r.Products)
            .Where(r => r.ClientId == query.ClientId && r.TenantId == TenantConstants.DefaultTenantId)
            .Join(
                db.Bookings,
                r => r.BookingId,
                b => b.Id,
                (r, b) => new { Recipe = r, Booking = b })
            .Join(
                db.StaffMembers,
                rb => rb.Recipe.StaffMemberId,
                s => s.Id,
                (rb, s) => new { rb.Recipe, rb.Booking, StaffMember = s })
            .OrderByDescending(x => x.Recipe.CreatedAtUtc)
            .Select(x => new ClientRecipeSummaryResponse(
                x.Recipe.Id,
                x.Recipe.BookingId,
                x.Booking.StartTime,
                x.Recipe.StaffMemberId,
                x.StaffMember.FirstName + " " + x.StaffMember.LastName,
                x.Recipe.Title,
                x.Recipe.Notes,
                x.Recipe.Products.OrderBy(p => p.SortOrder).Select(p => new RecipeProductResponse(
                    p.Id,
                    p.Name,
                    p.Brand,
                    p.Quantity,
                    p.SortOrder)).ToList(),
                x.Recipe.CreatedAtUtc,
                x.Recipe.UpdatedAtUtc))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return recipes;
    }
}
#pragma warning restore CA1812
