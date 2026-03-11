using Chairly.Api.Shared.Mediator;

namespace Chairly.Api.Features.Settings.GetCompanyInfo;

internal static class GetCompanyInfoEndpoint
{
    public static void MapGetCompanyInfo(this RouteGroupBuilder group)
    {
        group.MapGet("/", async (
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new GetCompanyInfoQuery(), cancellationToken).ConfigureAwait(false);
            return Results.Ok(result);
        });
    }
}
