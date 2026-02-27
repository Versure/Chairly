using Chairly.Api.Shared.Mediator;

namespace Chairly.Api.Features.Services.DeleteServiceCategory;

internal static class DeleteServiceCategoryEndpoint
{
    public static void MapDeleteServiceCategory(this RouteGroupBuilder group)
    {
        group.MapDelete("/{id:guid}", async (
            Guid id,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var command = new DeleteServiceCategoryCommand { Id = id };
            var result = await mediator.Send(command, cancellationToken).ConfigureAwait(false);
            return result.Match(
                _ => Results.NoContent(),
                _ => Results.NotFound());
        });
    }
}
