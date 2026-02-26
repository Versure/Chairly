using Chairly.Api.Dispatching;

namespace Chairly.Api.Features.Services.UpdateServiceCategory;

internal static class UpdateServiceCategoryEndpoint
{
    public static void MapUpdateServiceCategory(this RouteGroupBuilder group)
    {
        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateServiceCategoryCommand command,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            command.Id = id;
            var result = await mediator.Send(command, cancellationToken).ConfigureAwait(false);
            return result.Match(
                category => Results.Ok(category),
                _ => Results.NotFound());
        });
    }
}
