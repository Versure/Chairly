using Chairly.Api.Shared.Mediator;

namespace Chairly.Api.Features.Services.GetServiceCategoriesList;

internal static class GetServiceCategoriesListEndpoint
{
    public static void MapGetServiceCategoriesList(this RouteGroupBuilder group)
    {
        group.MapGet("/", async (
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new GetServiceCategoriesListQuery(), cancellationToken).ConfigureAwait(false);
            return Results.Ok(result);
        });
    }
}
