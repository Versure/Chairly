namespace Chairly.Infrastructure.Keycloak;

public interface IKeycloakAdminService
{
    Task<string> CreateRealmAsync(Guid tenantId, string adminEmail, CancellationToken ct = default);

    Task<string> CreateUserAsync(Guid tenantId, string email, string firstName, string lastName,
        string role, CancellationToken ct = default);

    Task UpdateUserAsync(Guid tenantId, string keycloakUserId, string email,
        string firstName, string lastName, CancellationToken ct = default);

    Task DisableUserAsync(Guid tenantId, string keycloakUserId, CancellationToken ct = default);

    Task EnableUserAsync(Guid tenantId, string keycloakUserId, CancellationToken ct = default);

    Task DeleteUserAsync(Guid tenantId, string keycloakUserId, CancellationToken ct = default);

    Task AssignRealmRoleAsync(Guid tenantId, string keycloakUserId, string roleName,
        CancellationToken ct = default);

    Task SetPasswordAsync(Guid tenantId, string keycloakUserId, string password,
        bool temporary = false, CancellationToken ct = default);

    Task SendActionsEmailAsync(Guid tenantId, string keycloakUserId, string[] actions,
        CancellationToken ct = default);

    Task DeleteRealmAsync(Guid tenantId, CancellationToken ct = default);
}
