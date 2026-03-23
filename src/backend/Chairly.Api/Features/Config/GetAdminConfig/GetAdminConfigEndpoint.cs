using Chairly.Api.Shared.Mediator;

namespace Chairly.Api.Features.Config.GetAdminConfig;

internal static class GetAdminConfigEndpoint
{
    public static void MapGetAdminConfig(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/config/admin", async (
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new GetAdminConfigQuery(), cancellationToken).ConfigureAwait(false);
            return Results.Ok(result);
        }).AllowAnonymous();
    }
}
