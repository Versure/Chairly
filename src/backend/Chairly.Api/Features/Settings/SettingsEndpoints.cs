using Chairly.Api.Features.Settings.GetCompanyInfo;
using Chairly.Api.Features.Settings.UpdateCompanyInfo;

namespace Chairly.Api.Features.Settings;

internal static class SettingsEndpoints
{
    public static IEndpointRouteBuilder MapSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/settings/company");

        group.MapGetCompanyInfo();
        group.MapUpdateCompanyInfo();

        return app;
    }
}
