using Chairly.Api.Shared.Mediator;

namespace Chairly.Api.Features.Services.CreateServiceCategory;

internal static class CreateServiceCategoryEndpoint
{
    public static void MapCreateServiceCategory(this RouteGroupBuilder group)
    {
        group.MapPost("/", async (
            CreateServiceCategoryCommand command,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(command, cancellationToken).ConfigureAwait(false);
            return Results.Created($"/api/service-categories/{result.Id}", result);
        });
    }
}
