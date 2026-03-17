using Chairly.Api.Features.Config.GetConfig;

namespace Chairly.Api.Features.Config;

internal static class ConfigEndpoints
{
    public static IEndpointRouteBuilder MapConfigEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGetConfig();

        return app;
    }
}
