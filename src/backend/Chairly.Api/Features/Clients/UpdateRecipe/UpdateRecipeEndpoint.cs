using Chairly.Api.Shared.Mediator;

namespace Chairly.Api.Features.Clients.UpdateRecipe;

internal static class UpdateRecipeEndpoint
{
    public static void MapUpdateRecipe(this RouteGroupBuilder group)
    {
        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateRecipeCommand command,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            command.Id = id;
            var result = await mediator.Send(command, cancellationToken).ConfigureAwait(false);
            return result.Match(
                recipe => Results.Ok(recipe),
                _ => Results.NotFound(),
                _ => Results.Forbid());
        });
    }
}
