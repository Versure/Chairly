using Chairly.Api.Features.Config.GetAdminConfig;
using Chairly.Api.Features.Config.GetConfig;

namespace Chairly.Api.Features.Config;

internal static class ConfigEndpoints
{
    public static IEndpointRouteBuilder MapConfigEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGetConfig();
        app.MapGetAdminConfig();

        return app;
    }
}
