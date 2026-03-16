using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Chairly.Api.Shared.Tenancy;

internal static partial class KeycloakDevSeeder
{
    private const string DefaultEmail = "manager@chairly.local";
    private const string DefaultPassword = "Chairly123!";
    private const string DefaultFirstName = "Chairly";
    private const string DefaultLastName = "Manager";
    private const string DefaultRole = "manager";

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static async Task SeedAsync(
        IServiceProvider services,
        IConfiguration configuration,
        CancellationToken ct)
    {
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("KeycloakDevSeeder");
        var httpClientFactory = services.GetRequiredService<IHttpClientFactory>();

        var keycloakUrl = configuration["Keycloak:Url"]
            ?? throw new InvalidOperationException("Keycloak:Url is required for dev seeding.");
        var realmString = configuration["Keycloak:Realm"]
            ?? throw new InvalidOperationException("Keycloak:Realm is required for dev seeding.");
        var adminPassword = configuration["Keycloak:AdminPassword"]
            ?? throw new InvalidOperationException("Keycloak:AdminPassword is required for dev seeding.");
        var frontendClientId = configuration["Keycloak:ClientId"] ?? "chairly-frontend";
        var adminClientId = configuration["Keycloak:AdminClientId"] ?? "chairly-admin";
        var adminClientSecret = configuration["Keycloak:AdminClientSecret"] ?? "";

        var tenantId = Guid.Parse(realmString);

        // Get admin token using the built-in admin-cli client with password grant.
        // This works on a fresh Keycloak without any pre-existing service accounts.
        var token = await GetAdminTokenAsync(httpClientFactory, keycloakUrl, adminPassword, ct).ConfigureAwait(false);

        // Step 1: Create realm (skip if already exists).
        await CreateRealmAsync(httpClientFactory, token, keycloakUrl, tenantId,
            frontendClientId, adminClientId, adminClientSecret, logger, ct).ConfigureAwait(false);

        // Step 2: Create user (skip if already exists).
        var userId = await CreateUserAsync(httpClientFactory, token, keycloakUrl, tenantId, logger, ct).ConfigureAwait(false);

        if (userId is null)
        {
            LogSeedComplete(logger, DefaultEmail, DefaultPassword);
            return;
        }

        // Step 3: Set password.
        await SetPasswordAsync(httpClientFactory, token, keycloakUrl, tenantId, userId, ct).ConfigureAwait(false);
        LogPasswordSet(logger, DefaultEmail);

        // Step 4: Assign manager role.
        await AssignRoleAsync(httpClientFactory, token, keycloakUrl, tenantId, userId, logger, ct).ConfigureAwait(false);

        LogSeedComplete(logger, DefaultEmail, DefaultPassword);
    }

    private static async Task<string> GetAdminTokenAsync(
        IHttpClientFactory httpClientFactory, string keycloakUrl, string adminPassword, CancellationToken ct)
    {
        using var client = httpClientFactory.CreateClient();
        using var request = new FormUrlEncodedContent(new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["grant_type"] = "password",
            ["client_id"] = "admin-cli",
            ["username"] = "admin",
            ["password"] = adminPassword,
        });

        var response = await client.PostAsync(
            new Uri($"{keycloakUrl}/realms/master/protocol/openid-connect/token"),
            request, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var tokenResponse = await response.Content.ReadFromJsonAsync<JsonElement>(ct).ConfigureAwait(false);
        return tokenResponse.GetProperty("access_token").GetString()!;
    }

    private static async Task<bool> CreateRealmAsync(
        IHttpClientFactory httpClientFactory, string token, string keycloakUrl,
        Guid tenantId, string frontendClientId, string adminClientId, string adminClientSecret,
        ILogger logger, CancellationToken ct)
    {
        using var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var realm = new
        {
            realm = tenantId.ToString(),
            displayName = "Chairly",
            loginTheme = "chairly",
            enabled = true,
            clients = new object[]
            {
                new
                {
                    clientId = frontendClientId,
                    publicClient = true,
                    standardFlowEnabled = true,
                    directAccessGrantsEnabled = false,
                    redirectUris = new[] { "*" },
                    webOrigins = new[] { "*" },
                },
                new
                {
                    clientId = adminClientId,
                    publicClient = false,
                    serviceAccountsEnabled = true,
                    standardFlowEnabled = false,
                    directAccessGrantsEnabled = false,
                    clientAuthenticatorType = "client-secret",
                    secret = adminClientSecret,
                },
            },
            roles = new
            {
                realm = new object[]
                {
                    new { name = "owner" },
                    new { name = "manager" },
                    new { name = "staff_member" },
                },
            },
        };

        var response = await client.PostAsJsonAsync($"{keycloakUrl}/admin/realms", realm, _jsonOptions, ct).ConfigureAwait(false);

        if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            LogRealmAlreadyExists(logger, tenantId);
            return false;
        }

        response.EnsureSuccessStatusCode();
        LogRealmCreated(logger, tenantId);
        return true;
    }

    private static async Task<string?> CreateUserAsync(
        IHttpClientFactory httpClientFactory, string token, string keycloakUrl,
        Guid tenantId, ILogger logger, CancellationToken ct)
    {
        using var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var user = new
        {
            username = DefaultEmail,
            email = DefaultEmail,
            firstName = DefaultFirstName,
            lastName = DefaultLastName,
            enabled = true,
        };

        var response = await client.PostAsJsonAsync(
            $"{keycloakUrl}/admin/realms/{tenantId}/users", user, _jsonOptions, ct).ConfigureAwait(false);

        if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            LogUserAlreadyExists(logger, DefaultEmail);
            return null;
        }

        response.EnsureSuccessStatusCode();
        LogUserCreated(logger, DefaultEmail);

        var location = response.Headers.Location?.ToString()
            ?? throw new InvalidOperationException("Keycloak did not return a Location header.");
        return location[(location.LastIndexOf('/') + 1)..];
    }

    private static async Task SetPasswordAsync(
        IHttpClientFactory httpClientFactory, string token, string keycloakUrl,
        Guid tenantId, string userId, CancellationToken ct)
    {
        using var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var credential = new { type = "password", value = DefaultPassword, temporary = false };

        var response = await client.PutAsJsonAsync(
            $"{keycloakUrl}/admin/realms/{tenantId}/users/{userId}/reset-password",
            credential, _jsonOptions, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    private static async Task AssignRoleAsync(
        IHttpClientFactory httpClientFactory, string token, string keycloakUrl,
        Guid tenantId, string userId, ILogger logger, CancellationToken ct)
    {
        using var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Get role representation first.
        var roleResponse = await client.GetAsync(
            new Uri($"{keycloakUrl}/admin/realms/{tenantId}/roles/{DefaultRole}"), ct).ConfigureAwait(false);
        roleResponse.EnsureSuccessStatusCode();
        var roleJson = await roleResponse.Content.ReadFromJsonAsync<JsonElement>(ct).ConfigureAwait(false);

        // Assign role to user.
        var response = await client.PostAsJsonAsync(
            $"{keycloakUrl}/admin/realms/{tenantId}/users/{userId}/role-mappings/realm",
            new[] { roleJson }, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        LogRoleAssigned(logger, DefaultRole, DefaultEmail);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Keycloak dev seed: created realm {TenantId}")]
    private static partial void LogRealmCreated(ILogger logger, Guid tenantId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Keycloak dev seed: realm {TenantId} already exists, skipping creation")]
    private static partial void LogRealmAlreadyExists(ILogger logger, Guid tenantId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Keycloak dev seed: created user {Email}")]
    private static partial void LogUserCreated(ILogger logger, string email);

    [LoggerMessage(Level = LogLevel.Information, Message = "Keycloak dev seed: user {Email} already exists, skipping setup")]
    private static partial void LogUserAlreadyExists(ILogger logger, string email);

    [LoggerMessage(Level = LogLevel.Information, Message = "Keycloak dev seed: password set for {Email}")]
    private static partial void LogPasswordSet(ILogger logger, string email);

    [LoggerMessage(Level = LogLevel.Information, Message = "Keycloak dev seed: assigned role '{Role}' to {Email}")]
    private static partial void LogRoleAssigned(ILogger logger, string role, string email);

    [LoggerMessage(Level = LogLevel.Information, Message = "Keycloak dev seed complete. Login: {Email} / {Password}")]
    private static partial void LogSeedComplete(ILogger logger, string email, string password);
}
