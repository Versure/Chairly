using Chairly.Api.Shared.Mediator;

namespace Chairly.Api.Features.Settings.GetVatSettings;

internal static class GetVatSettingsEndpoint
{
    public static void MapGetVatSettings(this RouteGroupBuilder group)
    {
        group.MapGet("/vat", async (
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new GetVatSettingsQuery(), cancellationToken).ConfigureAwait(false);
            return Results.Ok(result);
        });
    }
}
