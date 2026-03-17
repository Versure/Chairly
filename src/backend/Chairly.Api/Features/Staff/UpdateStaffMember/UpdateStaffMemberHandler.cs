using System.Text.Json;
using Chairly.Api.Features.Staff.CreateStaffMember;
using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Tenancy;
using Chairly.Infrastructure.Keycloak;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OneOf;
using OneOf.Types;

namespace Chairly.Api.Features.Staff.UpdateStaffMember;

#pragma warning disable CA1812
internal sealed partial class UpdateStaffMemberHandler(
    ChairlyDbContext db,
    IKeycloakAdminService keycloakAdmin,
    ILogger<UpdateStaffMemberHandler> logger,
    ITenantContext tenantContext) : IRequestHandler<UpdateStaffMemberCommand, OneOf<StaffMemberResponse, NotFound>>
{
    public async Task<OneOf<StaffMemberResponse, NotFound>> Handle(UpdateStaffMemberCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var member = await db.StaffMembers
            .FirstOrDefaultAsync(s => s.Id == command.Id && s.TenantId == tenantContext.TenantId, cancellationToken)
            .ConfigureAwait(false);

        if (member is null)
        {
            return new NotFound();
        }

        var emailChanged = command.Email is not null && !string.Equals(command.Email, member.Email, StringComparison.OrdinalIgnoreCase);
        var nameChanged = !string.Equals(command.FirstName, member.FirstName, StringComparison.Ordinal)
            || !string.Equals(command.LastName, member.LastName, StringComparison.Ordinal);

        member.FirstName = command.FirstName;
        member.LastName = command.LastName;
        if (command.Email is not null)
        {
            member.Email = command.Email;
        }

        member.Role = CreateStaffMemberHandler.ParseRole(command.Role);
        member.Color = command.Color;
        member.PhotoUrl = command.PhotoUrl;
        member.ScheduleJson = JsonSerializer.Serialize(command.Schedule ?? new Dictionary<string, ShiftBlockCommand[]>(StringComparer.OrdinalIgnoreCase));
        member.UpdatedAtUtc = DateTimeOffset.UtcNow;
        member.UpdatedBy = tenantContext.UserId;

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // Sync changes to Keycloak (non-fatal on failure).
        if ((emailChanged || nameChanged) && member.KeycloakUserId is not null)
        {
            try
            {
                await keycloakAdmin.UpdateUserAsync(
                    tenantContext.TenantId, member.KeycloakUserId, member.Email,
                    member.FirstName, member.LastName, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException)
            {
                LogKeycloakUpdateFailed(logger, member.Id, ex);
            }
        }

        return CreateStaffMemberHandler.ToResponse(member);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to update Keycloak user for staff member {StaffMemberId}; DB is source of truth")]
    private static partial void LogKeycloakUpdateFailed(ILogger logger, Guid staffMemberId, Exception exception);
}
#pragma warning restore CA1812
