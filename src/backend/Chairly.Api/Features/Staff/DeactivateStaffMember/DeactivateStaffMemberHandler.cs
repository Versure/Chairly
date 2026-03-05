using Chairly.Api.Features.Staff.CreateStaffMember;
using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Tenancy;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;

namespace Chairly.Api.Features.Staff.DeactivateStaffMember;

#pragma warning disable CA1812
internal sealed class DeactivateStaffMemberHandler(ChairlyDbContext db) : IRequestHandler<DeactivateStaffMemberCommand, OneOf<StaffMemberResponse, NotFound>>
{
    public async Task<OneOf<StaffMemberResponse, NotFound>> Handle(DeactivateStaffMemberCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var member = await db.StaffMembers
            .FirstOrDefaultAsync(s => s.Id == command.Id && s.TenantId == TenantConstants.DefaultTenantId, cancellationToken)
            .ConfigureAwait(false);

        if (member is null)
        {
            return new NotFound();
        }

        if (member.DeactivatedAtUtc != null)
        {
            return CreateStaffMemberHandler.ToResponse(member);
        }

        member.DeactivatedAtUtc = DateTimeOffset.UtcNow;
#pragma warning disable MA0026 // TODO: Replace with authenticated user ID from Keycloak
        member.DeactivatedBy = Guid.Empty;
        member.UpdatedAtUtc = DateTimeOffset.UtcNow;
        member.UpdatedBy = Guid.Empty;
#pragma warning restore MA0026

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return CreateStaffMemberHandler.ToResponse(member);
    }
}
#pragma warning restore CA1812
