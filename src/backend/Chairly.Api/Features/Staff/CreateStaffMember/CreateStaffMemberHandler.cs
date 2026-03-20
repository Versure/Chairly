using System.Text.Json;
using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Results;
using Chairly.Api.Shared.Tenancy;
using Chairly.Domain.Entities;
using Chairly.Domain.Enums;
using Chairly.Infrastructure.Keycloak;
using Chairly.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;
using OneOf;

namespace Chairly.Api.Features.Staff.CreateStaffMember;

#pragma warning disable CA1812
internal sealed partial class CreateStaffMemberHandler(
    ChairlyDbContext db,
    IKeycloakAdminService keycloakAdmin,
    ILogger<CreateStaffMemberHandler> logger,
    ITenantContext tenantContext) : IRequestHandler<CreateStaffMemberCommand, OneOf<StaffMemberResponse, KeycloakError>>
{
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<OneOf<StaffMemberResponse, KeycloakError>> Handle(CreateStaffMemberCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var member = CreateStaffMember(command);

        db.StaffMembers.Add(member);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        string? createdKeycloakUserId = null;

        try
        {
            createdKeycloakUserId = await keycloakAdmin.CreateUserAsync(
                tenantContext.TenantId, command.Email, command.FirstName, command.LastName,
                MapRoleToString(member.Role), cancellationToken).ConfigureAwait(false);

            await keycloakAdmin.AssignRealmRoleAsync(
                tenantContext.TenantId, createdKeycloakUserId, MapRoleToString(member.Role),
                cancellationToken).ConfigureAwait(false);

            member.KeycloakUserId = createdKeycloakUserId;
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                await keycloakAdmin.SendActionsEmailAsync(
                    tenantContext.TenantId, createdKeycloakUserId, ["UPDATE_PASSWORD"],
                    cancellationToken).ConfigureAwait(false);
            }
            catch (Exception emailEx) when (emailEx is HttpRequestException or InvalidOperationException)
            {
                LogPasswordEmailFailed(logger, member.Id, emailEx);
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException)
        {
            LogKeycloakCreateFailed(logger, member.Id, ex);
            await CleanupKeycloakUserAsync(createdKeycloakUserId, member.Id, cancellationToken).ConfigureAwait(false);

            db.StaffMembers.Remove(member);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return new KeycloakError("Failed to create Keycloak user for staff member.");
        }

        return ToResponse(member);
    }

    private async Task CleanupKeycloakUserAsync(string? keycloakUserId, Guid staffMemberId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(keycloakUserId))
        {
            return;
        }

        try
        {
            await keycloakAdmin.DeleteUserAsync(tenantContext.TenantId, keycloakUserId, ct).ConfigureAwait(false);
        }
        catch (Exception cleanupEx) when (cleanupEx is HttpRequestException or InvalidOperationException)
        {
            LogKeycloakCleanupFailed(logger, staffMemberId, cleanupEx);
        }
    }

    private StaffMember CreateStaffMember(CreateStaffMemberCommand command)
    {
        var scheduleJson = JsonSerializer.Serialize(command.Schedule ?? new Dictionary<string, ShiftBlockCommand[]>(StringComparer.OrdinalIgnoreCase));

        return new StaffMember
        {
            Id = Guid.NewGuid(),
            TenantId = tenantContext.TenantId,
            FirstName = command.FirstName,
            LastName = command.LastName,
            Email = command.Email,
            Role = ParseRole(command.Role),
            Color = command.Color,
            PhotoUrl = command.PhotoUrl,
            ScheduleJson = scheduleJson,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedBy = tenantContext.UserId,
        };
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
            member.Email,
            MapRoleToString(member.Role),
            member.Color,
            member.PhotoUrl,
            member.DeactivatedAtUtc == null,
            schedule,
            member.CreatedAtUtc,
            member.UpdatedAtUtc);
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to create Keycloak user for staff member {StaffMemberId}")]
    private static partial void LogKeycloakCreateFailed(ILogger logger, Guid staffMemberId, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to clean up Keycloak user after create failure for staff member {StaffMemberId}")]
    private static partial void LogKeycloakCleanupFailed(ILogger logger, Guid staffMemberId, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to send password setup email for staff member {StaffMemberId}")]
    private static partial void LogPasswordEmailFailed(ILogger logger, Guid staffMemberId, Exception exception);
}
#pragma warning restore CA1812
