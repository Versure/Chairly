using Chairly.Api.Features.Tenants.ProvisionTenant;

namespace Chairly.Api.Features.Tenants;

internal static class TenantEndpoints
{
    public static IEndpointRouteBuilder MapTenantEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapProvisionTenant();

        return app;
    }
}
