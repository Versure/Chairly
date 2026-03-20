using Chairly.Infrastructure.Keycloak;

namespace Chairly.Tests.Helpers;

internal sealed class SendEmailFailingKeycloakAdminService : IKeycloakAdminService
{
    private readonly string _createdUserId = Guid.NewGuid().ToString();

    public Task<string> CreateRealmAsync(Guid tenantId, string adminEmail, CancellationToken ct = default)
        => Task.FromResult(tenantId.ToString());

    public Task<string> CreateUserAsync(Guid tenantId, string email, string firstName, string lastName,
        string role, CancellationToken ct = default)
        => Task.FromResult(_createdUserId);

    public Task UpdateUserAsync(Guid tenantId, string keycloakUserId, string email,
        string firstName, string lastName, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task DisableUserAsync(Guid tenantId, string keycloakUserId, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task EnableUserAsync(Guid tenantId, string keycloakUserId, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task DeleteUserAsync(Guid tenantId, string keycloakUserId, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task DeleteRealmAsync(Guid tenantId, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task SetPasswordAsync(Guid tenantId, string keycloakUserId, string password,
        bool temporary = false, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task AssignRealmRoleAsync(Guid tenantId, string keycloakUserId, string roleName,
        CancellationToken ct = default)
        => Task.CompletedTask;

    public Task SendActionsEmailAsync(Guid tenantId, string keycloakUserId, string[] actions,
        CancellationToken ct = default)
        => throw new HttpRequestException("SMTP not configured");
}
