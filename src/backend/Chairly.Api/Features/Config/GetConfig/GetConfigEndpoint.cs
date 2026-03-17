using Chairly.Api.Shared.Mediator;

namespace Chairly.Api.Features.Config.GetConfig;

internal static class GetConfigEndpoint
{
    public static void MapGetConfig(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/config", async (
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new GetConfigQuery(), cancellationToken).ConfigureAwait(false);
            return Results.Ok(result);
        }).AllowAnonymous();
    }
}
