using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Tenancy;
using Chairly.Infrastructure.Keycloak;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OneOf;
using OneOf.Types;

#pragma warning disable CA1812
namespace Chairly.Api.Features.Staff.ResetStaffPassword;

internal sealed partial class ResetStaffPasswordHandler(
    ChairlyDbContext db,
    IKeycloakAdminService keycloakAdmin,
    ILogger<ResetStaffPasswordHandler> logger,
    ITenantContext tenantContext) : IRequestHandler<ResetStaffPasswordCommand, OneOf<Success, NotFound>>
{
    public async Task<OneOf<Success, NotFound>> Handle(ResetStaffPasswordCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var member = await db.StaffMembers
            .FirstOrDefaultAsync(s => s.Id == command.Id && s.TenantId == tenantContext.TenantId, cancellationToken)
            .ConfigureAwait(false);

        if (member is null)
        {
            return new NotFound();
        }

        if (member.KeycloakUserId is null)
        {
            return new NotFound();
        }

        try
        {
            await keycloakAdmin.SendActionsEmailAsync(
                tenantContext.TenantId, member.KeycloakUserId, ["UPDATE_PASSWORD"], cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException)
        {
            LogKeycloakResetFailed(logger, member.Id, ex);
        }

        return new Success();
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to send password reset email for staff member {StaffMemberId}; Keycloak may be unreachable")]
    private static partial void LogKeycloakResetFailed(ILogger logger, Guid staffMemberId, Exception exception);
}
#pragma warning restore CA1812
