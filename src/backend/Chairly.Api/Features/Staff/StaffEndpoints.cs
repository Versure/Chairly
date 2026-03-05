using Chairly.Api.Features.Staff.GetStaffList;

namespace Chairly.Api.Features.Staff;

internal static class StaffEndpoints
{
    public static IEndpointRouteBuilder MapStaffEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/staff");

        group.MapGetStaffList();

        return app;
    }
}
