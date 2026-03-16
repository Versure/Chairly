using Chairly.Api.Features.Staff.CreateStaffMember;
using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Tenancy;
using Chairly.Infrastructure.Keycloak;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OneOf;
using OneOf.Types;

namespace Chairly.Api.Features.Staff.DeactivateStaffMember;

#pragma warning disable CA1812
internal sealed partial class DeactivateStaffMemberHandler(
    ChairlyDbContext db,
    IKeycloakAdminService keycloakAdmin,
    ILogger<DeactivateStaffMemberHandler> logger,
    ITenantContext tenantContext) : IRequestHandler<DeactivateStaffMemberCommand, OneOf<StaffMemberResponse, NotFound>>
{
    public async Task<OneOf<StaffMemberResponse, NotFound>> Handle(DeactivateStaffMemberCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var member = await db.StaffMembers
            .FirstOrDefaultAsync(s => s.Id == command.Id && s.TenantId == tenantContext.TenantId, cancellationToken)
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
        member.DeactivatedBy = tenantContext.UserId;
        member.UpdatedAtUtc = DateTimeOffset.UtcNow;
        member.UpdatedBy = tenantContext.UserId;

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // Disable the Keycloak user (non-fatal on failure).
        if (member.KeycloakUserId is not null)
        {
            try
            {
                await keycloakAdmin.DisableUserAsync(
                    tenantContext.TenantId, member.KeycloakUserId, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException)
            {
                LogKeycloakDisableFailed(logger, member.Id, ex);
            }
        }

        return CreateStaffMemberHandler.ToResponse(member);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to disable Keycloak user for staff member {StaffMemberId}; DB is source of truth")]
    private static partial void LogKeycloakDisableFailed(ILogger logger, Guid staffMemberId, Exception exception);
}
#pragma warning restore CA1812
