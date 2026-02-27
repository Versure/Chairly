using Chairly.Api.Shared.Mediator;

namespace Chairly.Api.Features.Services.CreateService;

internal static class CreateServiceEndpoint
{
    public static void MapCreateService(this RouteGroupBuilder group)
    {
        group.MapPost("/", async (
            CreateServiceCommand command,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(command, cancellationToken).ConfigureAwait(false);
            return Results.Created($"/api/services/{result.Id}", result);
        });
    }
}
