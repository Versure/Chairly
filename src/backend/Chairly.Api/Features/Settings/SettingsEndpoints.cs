using Chairly.Api.Features.Settings.GetCompanyInfo;
using Chairly.Api.Features.Settings.GetVatSettings;
using Chairly.Api.Features.Settings.UpdateCompanyInfo;
using Chairly.Api.Features.Settings.UpdateVatSettings;

namespace Chairly.Api.Features.Settings;

internal static class SettingsEndpoints
{
    public static IEndpointRouteBuilder MapSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        var settingsGroup = app.MapGroup("/api/settings");

        settingsGroup.MapGetVatSettings();
        settingsGroup.MapUpdateVatSettings();

        var companyGroup = app.MapGroup("/api/settings/company");

        companyGroup.MapGetCompanyInfo();
        companyGroup.MapUpdateCompanyInfo();

        return app;
    }
}
