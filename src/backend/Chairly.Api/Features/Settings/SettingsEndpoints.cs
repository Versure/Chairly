using Chairly.Api.Features.Settings.GetVatSettings;
using Chairly.Api.Features.Settings.UpdateVatSettings;

namespace Chairly.Api.Features.Settings;

internal static class SettingsEndpoints
{
    public static IEndpointRouteBuilder MapSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/settings");

        group.MapGetVatSettings();
        group.MapUpdateVatSettings();

        return app;
    }
}
