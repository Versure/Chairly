using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Results;
using Chairly.Infrastructure.Keycloak;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneOf;

namespace Chairly.Api.Features.Tenants.ProvisionTenant;

#pragma warning disable CA1812
internal sealed partial class ProvisionTenantHandler(
    IKeycloakAdminService keycloakAdmin,
    IConfiguration configuration,
    ILogger<ProvisionTenantHandler> logger) : IRequestHandler<ProvisionTenantCommand, OneOf<ProvisionTenantResponse, Unprocessable>>
{
    public async Task<OneOf<ProvisionTenantResponse, Unprocessable>> Handle(ProvisionTenantCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.TenantId == Guid.Empty)
        {
            return new Unprocessable("TenantId must not be empty.");
        }

        string? realmCreated = null;

        try
        {
            realmCreated = await keycloakAdmin.CreateRealmAsync(command.TenantId, command.OwnerEmail, cancellationToken).ConfigureAwait(false);

            var keycloakUserId = await keycloakAdmin.CreateUserAsync(
                command.TenantId,
                command.OwnerEmail,
                command.OwnerFirstName,
                command.OwnerLastName,
                "owner",
                cancellationToken).ConfigureAwait(false);

            await keycloakAdmin.AssignRealmRoleAsync(command.TenantId, keycloakUserId, "owner", cancellationToken).ConfigureAwait(false);

            var keycloakUrl = configuration["Keycloak:Url"]!;
            var loginUrl = $"{keycloakUrl}/realms/{command.TenantId}/account";

            return new ProvisionTenantResponse(command.TenantId, keycloakUserId, loginUrl);
        }
#pragma warning disable CA1031 // Catch all for cleanup on Keycloak provisioning failure
        catch (Exception ex)
#pragma warning restore CA1031
        {
            LogProvisioningFailed(logger, command.TenantId, ex);

            // Attempt cleanup: delete the entire realm if it was created.
            // Deleting the realm also removes all users and roles within it.
            if (realmCreated is not null)
            {
                try
                {
                    await keycloakAdmin.DeleteRealmAsync(command.TenantId, CancellationToken.None).ConfigureAwait(false);
                }
#pragma warning disable CA1031 // Best-effort cleanup
                catch (Exception cleanupEx)
#pragma warning restore CA1031
                {
                    LogCleanupFailed(logger, command.TenantId, cleanupEx);
                }
            }

            return new Unprocessable($"Keycloak provisioning failed: {ex.Message}");
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to provision tenant {TenantId}")]
    private static partial void LogProvisioningFailed(ILogger logger, Guid tenantId, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to clean up after failed provisioning for tenant {TenantId}")]
    private static partial void LogCleanupFailed(ILogger logger, Guid tenantId, Exception exception);
}
#pragma warning restore CA1812
