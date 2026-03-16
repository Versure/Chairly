using Chairly.Infrastructure.Keycloak;

namespace Chairly.Tests.Helpers;

internal sealed class NullKeycloakAdminService : IKeycloakAdminService
{
    public string LastCreatedUserId { get; private set; } = string.Empty;
    public bool CreateUserCalled { get; private set; }
    public bool AssignRoleCalled { get; private set; }
    public bool UpdateUserCalled { get; private set; }
    public bool DisableUserCalled { get; private set; }
    public bool EnableUserCalled { get; private set; }
    public bool DeleteRealmCalled { get; private set; }

    public Task<string> CreateRealmAsync(Guid tenantId, string adminEmail, CancellationToken ct = default)
        => Task.FromResult(tenantId.ToString());

    public Task<string> CreateUserAsync(Guid tenantId, string email, string firstName, string lastName,
        string role, CancellationToken ct = default)
    {
        CreateUserCalled = true;
        LastCreatedUserId = Guid.NewGuid().ToString();
        return Task.FromResult(LastCreatedUserId);
    }

    public Task UpdateUserAsync(Guid tenantId, string keycloakUserId, string email,
        string firstName, string lastName, CancellationToken ct = default)
    {
        UpdateUserCalled = true;
        return Task.CompletedTask;
    }

    public Task DisableUserAsync(Guid tenantId, string keycloakUserId, CancellationToken ct = default)
    {
        DisableUserCalled = true;
        return Task.CompletedTask;
    }

    public Task EnableUserAsync(Guid tenantId, string keycloakUserId, CancellationToken ct = default)
    {
        EnableUserCalled = true;
        return Task.CompletedTask;
    }

    public Task DeleteUserAsync(Guid tenantId, string keycloakUserId, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task DeleteRealmAsync(Guid tenantId, CancellationToken ct = default)
    {
        DeleteRealmCalled = true;
        return Task.CompletedTask;
    }

    public Task SetPasswordAsync(Guid tenantId, string keycloakUserId, string password,
        bool temporary = false, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task AssignRealmRoleAsync(Guid tenantId, string keycloakUserId, string roleName,
        CancellationToken ct = default)
    {
        AssignRoleCalled = true;
        return Task.CompletedTask;
    }
}
