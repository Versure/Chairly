using Chairly.Api.Features.Staff.CreateStaffMember;
using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Tenancy;
using Chairly.Infrastructure.Keycloak;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OneOf;
using OneOf.Types;

namespace Chairly.Api.Features.Staff.ReactivateStaffMember;

#pragma warning disable CA1812
internal sealed partial class ReactivateStaffMemberHandler(
    ChairlyDbContext db,
    IKeycloakAdminService keycloakAdmin,
    ILogger<ReactivateStaffMemberHandler> logger,
    ITenantContext tenantContext) : IRequestHandler<ReactivateStaffMemberCommand, OneOf<StaffMemberResponse, NotFound>>
{
    public async Task<OneOf<StaffMemberResponse, NotFound>> Handle(ReactivateStaffMemberCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var member = await db.StaffMembers
            .FirstOrDefaultAsync(s => s.Id == command.Id && s.TenantId == tenantContext.TenantId, cancellationToken)
            .ConfigureAwait(false);

        if (member is null)
        {
            return new NotFound();
        }

        member.DeactivatedAtUtc = null;
        member.DeactivatedBy = null;
        member.UpdatedAtUtc = DateTimeOffset.UtcNow;
        member.UpdatedBy = tenantContext.UserId;

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // Re-enable the Keycloak user (non-fatal on failure).
        if (member.KeycloakUserId is not null)
        {
            try
            {
                await keycloakAdmin.EnableUserAsync(
                    tenantContext.TenantId, member.KeycloakUserId, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException)
            {
                LogKeycloakEnableFailed(logger, member.Id, ex);
            }
        }

        return CreateStaffMemberHandler.ToResponse(member);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to enable Keycloak user for staff member {StaffMemberId}; DB is source of truth")]
    private static partial void LogKeycloakEnableFailed(ILogger logger, Guid staffMemberId, Exception exception);
}
#pragma warning restore CA1812
