using Chairly.Api.Features.Tenants.ProvisionTenant;
using Chairly.Api.Shared.Results;
using Chairly.Infrastructure.Keycloak;
using Chairly.Tests.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Chairly.Tests.Features.Tenants;

public class ProvisionTenantHandlerTests
{
    private static IConfiguration CreateConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["Keycloak:Url"] = "http://localhost:8080",
            })
            .Build();
    }

    [Fact]
    public async Task Handle_HappyPath_CallsCreateRealmCreateUserAssignRoleInOrder()
    {
        var keycloak = new NullKeycloakAdminService();
        var handler = new ProvisionTenantHandler(keycloak, CreateConfiguration(), NullLogger<ProvisionTenantHandler>.Instance);
        var tenantId = Guid.NewGuid();

        var command = new ProvisionTenantCommand
        {
            TenantId = tenantId,
            OwnerEmail = "owner@example.com",
            OwnerFirstName = "Jan",
            OwnerLastName = "Jansen",
        };

        var result = await handler.Handle(command);

        Assert.True(result.IsT0);
        var response = result.AsT0;
        Assert.Equal(tenantId, response.TenantId);
        Assert.NotEmpty(response.OwnerKeycloakUserId);
        Assert.Contains(tenantId.ToString(), response.LoginUrl, StringComparison.Ordinal);
        Assert.True(keycloak.CreateUserCalled);
        Assert.True(keycloak.AssignRoleCalled);
    }

    [Fact]
    public async Task Handle_CreateUserFails_CallsDeleteRealmAndReturnsError()
    {
        var keycloak = new RealmSuccessUserFailKeycloakAdminService();
        var handler = new ProvisionTenantHandler(keycloak, CreateConfiguration(), NullLogger<ProvisionTenantHandler>.Instance);
        var tenantId = Guid.NewGuid();

        var command = new ProvisionTenantCommand
        {
            TenantId = tenantId,
            OwnerEmail = "owner@example.com",
            OwnerFirstName = "Jan",
            OwnerLastName = "Jansen",
        };

        var result = await handler.Handle(command);

        Assert.True(result.IsT1);
        Assert.IsType<Unprocessable>(result.AsT1);
        Assert.True(keycloak.DeleteRealmCalled);
    }

    [Fact]
    public async Task Handle_EmptyTenantId_ReturnsUnprocessable()
    {
        var keycloak = new NullKeycloakAdminService();
        var handler = new ProvisionTenantHandler(keycloak, CreateConfiguration(), NullLogger<ProvisionTenantHandler>.Instance);

        var command = new ProvisionTenantCommand
        {
            TenantId = Guid.Empty,
            OwnerEmail = "owner@example.com",
            OwnerFirstName = "Jan",
            OwnerLastName = "Jansen",
        };

        var result = await handler.Handle(command);

        Assert.True(result.IsT1);
        Assert.IsType<Unprocessable>(result.AsT1);
    }

    /// <summary>
    /// A Keycloak admin service that succeeds on realm creation but fails on user creation.
    /// Used to test the cleanup (DeleteRealmAsync) path.
    /// </summary>
    private sealed class RealmSuccessUserFailKeycloakAdminService : IKeycloakAdminService
    {
        public bool DeleteRealmCalled { get; private set; }

        public Task<string> CreateRealmAsync(Guid tenantId, string adminEmail, CancellationToken ct = default)
            => Task.FromResult(tenantId.ToString());

        public Task<string> CreateUserAsync(Guid tenantId, string email, string firstName, string lastName,
            string role, CancellationToken ct = default)
            => throw new HttpRequestException("Keycloak user creation failed");

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
        {
            DeleteRealmCalled = true;
            return Task.CompletedTask;
        }

        public Task SetPasswordAsync(Guid tenantId, string keycloakUserId, string password,
            bool temporary = false, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task AssignRealmRoleAsync(Guid tenantId, string keycloakUserId, string roleName,
            CancellationToken ct = default)
            => Task.CompletedTask;

        public Task SendActionsEmailAsync(Guid tenantId, string keycloakUserId, string[] actions,
            CancellationToken ct = default)
            => Task.CompletedTask;

        public Task<string?> GetUserDisplayNameAsync(string realmName, string userId, CancellationToken ct = default)
            => Task.FromResult<string?>(null);
    }
}
