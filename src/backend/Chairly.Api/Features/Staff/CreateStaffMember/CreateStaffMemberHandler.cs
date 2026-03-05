using System.Text.Json;
using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Tenancy;
using Chairly.Domain.Entities;
using Chairly.Domain.Enums;
using Chairly.Infrastructure.Persistence;

namespace Chairly.Api.Features.Staff.CreateStaffMember;

#pragma warning disable CA1812
internal sealed class CreateStaffMemberHandler(ChairlyDbContext db) : IRequestHandler<CreateStaffMemberCommand, StaffMemberResponse>
{
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<StaffMemberResponse> Handle(CreateStaffMemberCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var scheduleJson = JsonSerializer.Serialize(command.Schedule ?? new Dictionary<string, ShiftBlockCommand[]>(StringComparer.OrdinalIgnoreCase));

        var member = new StaffMember
        {
            Id = Guid.NewGuid(),
            TenantId = TenantConstants.DefaultTenantId,
            FirstName = command.FirstName,
            LastName = command.LastName,
            Role = ParseRole(command.Role),
            Color = command.Color,
            PhotoUrl = command.PhotoUrl,
            ScheduleJson = scheduleJson,
            CreatedAtUtc = DateTimeOffset.UtcNow,
#pragma warning disable MA0026 // TODO: Replace with authenticated user ID from Keycloak
            CreatedBy = Guid.Empty,
#pragma warning restore MA0026
        };

        db.StaffMembers.Add(member);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return ToResponse(member);
    }

    internal static StaffRole ParseRole(string role) => role switch
    {
        "manager" => StaffRole.Manager,
        "staff_member" => StaffRole.StaffMember,
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, null),
    };

    internal static string MapRoleToString(StaffRole staffRole) => staffRole switch
    {
        StaffRole.Manager => "manager",
        StaffRole.StaffMember => "staff_member",
        _ => throw new ArgumentOutOfRangeException(nameof(staffRole), staffRole, null),
    };

    internal static StaffMemberResponse ToResponse(StaffMember member)
    {
        var schedule = JsonSerializer.Deserialize<Dictionary<string, ShiftBlockResponse[]>>(member.ScheduleJson, _jsonOptions)
            ?? new Dictionary<string, ShiftBlockResponse[]>(StringComparer.OrdinalIgnoreCase);

        return new StaffMemberResponse(
            member.Id,
            member.FirstName,
            member.LastName,
            MapRoleToString(member.Role),
            member.Color,
            member.PhotoUrl,
            member.DeactivatedAtUtc == null,
            schedule,
            member.CreatedAtUtc,
            member.UpdatedAtUtc);
    }
}
#pragma warning restore CA1812
