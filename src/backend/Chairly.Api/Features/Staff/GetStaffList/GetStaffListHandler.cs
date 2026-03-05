using Chairly.Api.Features.Staff.CreateStaffMember;
using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Tenancy;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Chairly.Api.Features.Staff.GetStaffList;

#pragma warning disable CA1812
internal sealed class GetStaffListHandler(ChairlyDbContext db) : IRequestHandler<GetStaffListQuery, IEnumerable<StaffMemberResponse>>
{
    public async Task<IEnumerable<StaffMemberResponse>> Handle(GetStaffListQuery query, CancellationToken cancellationToken = default)
    {
        var members = await db.StaffMembers
            .Where(s => s.TenantId == TenantConstants.DefaultTenantId)
            .OrderBy(s => s.FirstName)
            .ThenBy(s => s.LastName)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return members.Select(CreateStaffMemberHandler.ToResponse);
    }
}
#pragma warning restore CA1812
