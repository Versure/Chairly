using Chairly.Api.Shared.Mediator;

namespace Chairly.Api.Features.Settings.UpdateVatSettings;

internal static class UpdateVatSettingsEndpoint
{
    public static void MapUpdateVatSettings(this RouteGroupBuilder group)
    {
        group.MapPut("/vat", async (
            UpdateVatSettingsCommand command,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(command, cancellationToken).ConfigureAwait(false);
            return Results.Ok(result);
        });
    }
}
