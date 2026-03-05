using Chairly.Api.Features.Staff.CreateStaffMember;
using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Tenancy;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;

namespace Chairly.Api.Features.Staff.ReactivateStaffMember;

#pragma warning disable CA1812
internal sealed class ReactivateStaffMemberHandler(ChairlyDbContext db) : IRequestHandler<ReactivateStaffMemberCommand, OneOf<StaffMemberResponse, NotFound>>
{
    public async Task<OneOf<StaffMemberResponse, NotFound>> Handle(ReactivateStaffMemberCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var member = await db.StaffMembers
            .FirstOrDefaultAsync(s => s.Id == command.Id && s.TenantId == TenantConstants.DefaultTenantId, cancellationToken)
            .ConfigureAwait(false);

        if (member is null)
        {
            return new NotFound();
        }

        member.DeactivatedAtUtc = null;
        member.DeactivatedBy = null;
        member.UpdatedAtUtc = DateTimeOffset.UtcNow;
#pragma warning disable MA0026 // TODO: Replace with authenticated user ID from Keycloak
        member.UpdatedBy = Guid.Empty;
#pragma warning restore MA0026

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return CreateStaffMemberHandler.ToResponse(member);
    }
}
#pragma warning restore CA1812
