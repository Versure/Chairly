using Chairly.Api.Features.Clients.CreateRecipe;
using Chairly.Api.Features.Clients.GetRecipeByBooking;
using Chairly.Api.Features.Clients.UpdateRecipe;

namespace Chairly.Api.Features.Clients;

internal static class RecipeEndpoints
{
    public static IEndpointRouteBuilder MapRecipeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/recipes")
            .RequireAuthorization("RequireStaff");

        group.MapCreateRecipe();
        group.MapGetRecipeByBooking();
        group.MapUpdateRecipe();

        return app;
    }
}
