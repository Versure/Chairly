using System.Text.Json;
using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Tenancy;
using Chairly.Domain.Enums;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Chairly.Api.Features.Staff.GetStaffList;

#pragma warning disable CA1812
internal sealed class GetStaffListHandler(ChairlyDbContext db) : IRequestHandler<GetStaffListQuery, IEnumerable<StaffMemberResponse>>
{
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<IEnumerable<StaffMemberResponse>> Handle(GetStaffListQuery query, CancellationToken cancellationToken = default)
    {
        var members = await db.StaffMembers
            .Where(s => s.TenantId == TenantConstants.DefaultTenantId)
            .OrderBy(s => s.FirstName)
            .ThenBy(s => s.LastName)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return members.Select(s => new StaffMemberResponse(
            s.Id,
            s.FirstName,
            s.LastName,
            MapRole(s.Role),
            s.Color,
            s.PhotoUrl,
            s.DeactivatedAtUtc == null,
            JsonSerializer.Deserialize<Dictionary<string, ShiftBlockResponse[]>>(s.ScheduleJson, _jsonOptions) ?? new Dictionary<string, ShiftBlockResponse[]>(StringComparer.OrdinalIgnoreCase),
            s.CreatedAtUtc,
            s.UpdatedAtUtc));
    }

    private static string MapRole(StaffRole role) => role switch
    {
        StaffRole.Manager => "manager",
        StaffRole.StaffMember => "staff_member",
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, null),
    };
}
#pragma warning restore CA1812
