using Chairly.Api.Features.Staff.CreateStaffMember;
using Chairly.Api.Features.Staff.DeactivateStaffMember;
using Chairly.Api.Features.Staff.GetStaffList;
using Chairly.Api.Features.Staff.ReactivateStaffMember;
using Chairly.Api.Features.Staff.UpdateStaffMember;

namespace Chairly.Api.Features.Staff;

internal static class StaffEndpoints
{
    public static IEndpointRouteBuilder MapStaffEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/staff");

        group.MapGetStaffList();
        group.MapCreateStaffMember();
        group.MapUpdateStaffMember();
        group.MapDeactivateStaffMember();
        group.MapReactivateStaffMember();

        return app;
    }
}
