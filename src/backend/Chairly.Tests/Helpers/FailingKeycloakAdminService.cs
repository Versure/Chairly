using Chairly.Infrastructure.Keycloak;

namespace Chairly.Tests.Helpers;

internal sealed class FailingKeycloakAdminService : IKeycloakAdminService
{
    public Task<string> CreateRealmAsync(Guid tenantId, string adminEmail, CancellationToken ct = default)
        => throw new HttpRequestException("Keycloak unreachable");

    public Task<string> CreateUserAsync(Guid tenantId, string email, string firstName, string lastName,
        string role, CancellationToken ct = default)
        => throw new HttpRequestException("Keycloak unreachable");

    public Task UpdateUserAsync(Guid tenantId, string keycloakUserId, string email,
        string firstName, string lastName, CancellationToken ct = default)
        => throw new HttpRequestException("Keycloak unreachable");

    public Task DisableUserAsync(Guid tenantId, string keycloakUserId, CancellationToken ct = default)
        => throw new HttpRequestException("Keycloak unreachable");

    public Task EnableUserAsync(Guid tenantId, string keycloakUserId, CancellationToken ct = default)
        => throw new HttpRequestException("Keycloak unreachable");

    public Task DeleteUserAsync(Guid tenantId, string keycloakUserId, CancellationToken ct = default)
        => throw new HttpRequestException("Keycloak unreachable");

    public Task DeleteRealmAsync(Guid tenantId, CancellationToken ct = default)
        => throw new HttpRequestException("Keycloak unreachable");

    public Task AssignRealmRoleAsync(Guid tenantId, string keycloakUserId, string roleName,
        CancellationToken ct = default)
        => throw new HttpRequestException("Keycloak unreachable");
}
