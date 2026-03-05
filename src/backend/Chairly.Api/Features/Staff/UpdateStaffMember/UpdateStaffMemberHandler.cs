using System.Text.Json;
using Chairly.Api.Features.Staff.CreateStaffMember;
using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Tenancy;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;

namespace Chairly.Api.Features.Staff.UpdateStaffMember;

#pragma warning disable CA1812
internal sealed class UpdateStaffMemberHandler(ChairlyDbContext db) : IRequestHandler<UpdateStaffMemberCommand, OneOf<StaffMemberResponse, NotFound>>
{
    public async Task<OneOf<StaffMemberResponse, NotFound>> Handle(UpdateStaffMemberCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var member = await db.StaffMembers
            .FirstOrDefaultAsync(s => s.Id == command.Id && s.TenantId == TenantConstants.DefaultTenantId, cancellationToken)
            .ConfigureAwait(false);

        if (member is null)
        {
            return new NotFound();
        }

        member.FirstName = command.FirstName;
        member.LastName = command.LastName;
        member.Role = CreateStaffMemberHandler.ParseRole(command.Role);
        member.Color = command.Color;
        member.PhotoUrl = command.PhotoUrl;
        member.ScheduleJson = JsonSerializer.Serialize(command.Schedule ?? new Dictionary<string, ShiftBlockCommand[]>(StringComparer.OrdinalIgnoreCase));
        member.UpdatedAtUtc = DateTimeOffset.UtcNow;
#pragma warning disable MA0026 // TODO: Replace with authenticated user ID from Keycloak
        member.UpdatedBy = Guid.Empty;
#pragma warning restore MA0026

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return CreateStaffMemberHandler.ToResponse(member);
    }
}
#pragma warning restore CA1812
