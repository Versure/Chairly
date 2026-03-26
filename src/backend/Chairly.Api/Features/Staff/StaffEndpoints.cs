using Chairly.Api.Features.Staff.CreateStaffMember;
using Chairly.Api.Features.Staff.DeactivateStaffMember;
using Chairly.Api.Features.Staff.GetStaffList;
using Chairly.Api.Features.Staff.ReactivateStaffMember;
using Chairly.Api.Features.Staff.ResetStaffPassword;
using Chairly.Api.Features.Staff.UpdateStaffMember;

namespace Chairly.Api.Features.Staff;

internal static class StaffEndpoints
{
    public static IEndpointRouteBuilder MapStaffEndpoints(this IEndpointRouteBuilder app)
    {
        var readGroup = app.MapGroup("/api/staff")
            .RequireAuthorization("RequireStaff");

        readGroup.MapGetStaffList();

        var writeGroup = app.MapGroup("/api/staff")
            .RequireAuthorization("RequireManager");

        writeGroup.MapCreateStaffMember();
        writeGroup.MapUpdateStaffMember();
        writeGroup.MapDeactivateStaffMember();
        writeGroup.MapReactivateStaffMember();
        writeGroup.MapResetStaffPassword();

        return app;
    }
}
