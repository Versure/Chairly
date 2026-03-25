using Chairly.Api.Features.Dashboard.GetDashboard;

namespace Chairly.Api.Features.Dashboard;

internal static class DashboardEndpoints
{
    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/dashboard")
            .RequireAuthorization("RequireStaff");

        group.MapGetDashboard();

        return app;
    }
}
