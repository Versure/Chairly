using Chairly.Api.Features.Settings.GetCompanyInfo;
using Chairly.Api.Features.Settings.GetVatSettings;
using Chairly.Api.Features.Settings.UpdateCompanyInfo;
using Chairly.Api.Features.Settings.UpdateVatSettings;

namespace Chairly.Api.Features.Settings;

internal static class SettingsEndpoints
{
    public static IEndpointRouteBuilder MapSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        var settingsReadGroup = app.MapGroup("/api/settings")
            .RequireAuthorization("RequireStaff");

        settingsReadGroup.MapGetVatSettings();

        var settingsWriteGroup = app.MapGroup("/api/settings")
            .RequireAuthorization("RequireManager");

        settingsWriteGroup.MapUpdateVatSettings();

        var companyReadGroup = app.MapGroup("/api/settings/company")
            .RequireAuthorization("RequireStaff");

        companyReadGroup.MapGetCompanyInfo();

        var companyWriteGroup = app.MapGroup("/api/settings/company")
            .RequireAuthorization("RequireManager");

        companyWriteGroup.MapUpdateCompanyInfo();

        return app;
    }
}
