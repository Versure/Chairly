using Chairly.Api.Shared.Mediator;

namespace Chairly.Api.Features.Clients.CreateRecipe;

internal static class CreateRecipeEndpoint
{
    public static void MapCreateRecipe(this RouteGroupBuilder group)
    {
        group.MapPost("/", async (
            CreateRecipeCommand command,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(command, cancellationToken).ConfigureAwait(false);
            return result.Match(
                recipe => Results.Created($"/api/recipes/{recipe.Id}", recipe),
                _ => Results.NotFound(),
                unprocessable => Results.UnprocessableEntity(new { error = unprocessable.Message }),
                conflict => Results.Conflict(new { error = conflict.Message }),
                _ => Results.Forbid());
        });
    }
}
