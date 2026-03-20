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
        var realmName = configuration["Keycloak:Realm"]
            ?? throw new InvalidOperationException("Keycloak:Realm is required for dev seeding.");
        var adminPassword = configuration["Keycloak:AdminPassword"]
            ?? throw new InvalidOperationException("Keycloak:AdminPassword is required for dev seeding.");
        var frontendClientId = configuration["Keycloak:ClientId"] ?? "chairly-frontend";
        var adminClientId = configuration["Keycloak:AdminClientId"] ?? "chairly-admin";
        var adminClientSecret = configuration["Keycloak:AdminClientSecret"] ?? "";

        // Get admin token using the built-in admin-cli client with password grant.
        // This works on a fresh Keycloak without any pre-existing service accounts.
        // Includes retry logic because Keycloak may not be fully ready on first start.
        var token = await GetAdminTokenAsync(httpClientFactory, keycloakUrl, adminPassword, logger, ct).ConfigureAwait(false);

        // Step 1: Create realm (skip if already exists).
        await CreateRealmAsync(httpClientFactory, token, keycloakUrl, realmName,
            frontendClientId, adminClientId, adminClientSecret, logger, ct).ConfigureAwait(false);

        // Step 1b: Always ensure realm display name, login theme, and SMTP are set.
        var smtpHost = configuration["Smtp:Host"];
        var smtpPort = configuration["Smtp:Port"];
        await UpdateRealmSettingsAsync(httpClientFactory, token, keycloakUrl, realmName, smtpHost, smtpPort, logger, ct).ConfigureAwait(false);

        // Step 1c: Assign realm-management roles to the admin service account (idempotent).
        await AssignServiceAccountRolesAsync(httpClientFactory, token, keycloakUrl, realmName, adminClientId, logger, ct).ConfigureAwait(false);

        // Step 2: Create user (skip if already exists).
        var userId = await CreateUserAsync(httpClientFactory, token, keycloakUrl, realmName, logger, ct).ConfigureAwait(false);

        if (userId is null)
        {
            LogSeedComplete(logger, DefaultEmail, DefaultPassword);
            return;
        }

        // Step 3: Set password.
        await SetPasswordAsync(httpClientFactory, token, keycloakUrl, realmName, userId, ct).ConfigureAwait(false);
        LogPasswordSet(logger, DefaultEmail);

        // Step 4: Assign manager role.
        await AssignRoleAsync(httpClientFactory, token, keycloakUrl, realmName, userId, logger, ct).ConfigureAwait(false);

        LogSeedComplete(logger, DefaultEmail, DefaultPassword);
    }

    private static async Task<string> GetAdminTokenAsync(
        IHttpClientFactory httpClientFactory, string keycloakUrl, string adminPassword,
        ILogger logger, CancellationToken ct)
    {
        // Keycloak may not be fully ready even after Aspire reports the container as running.
        // Retry a few times with backoff so the app starts on the first attempt.
        const int maxAttempts = 5;
        const int delayMs = 2000;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
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
            catch (HttpRequestException) when (attempt < maxAttempts)
            {
                LogKeycloakNotReady(logger, attempt, maxAttempts);
                await Task.Delay(delayMs * attempt, ct).ConfigureAwait(false);
            }
        }

        throw new InvalidOperationException("Keycloak admin API did not become available after retries.");
    }

    private static async Task<bool> CreateRealmAsync(
        IHttpClientFactory httpClientFactory, string token, string keycloakUrl,
        string realmName, string frontendClientId, string adminClientId, string adminClientSecret,
        ILogger logger, CancellationToken ct)
    {
        using var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var realm = new
        {
            realm = realmName,
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
            LogRealmAlreadyExists(logger, realmName);
            return false;
        }

        response.EnsureSuccessStatusCode();
        LogRealmCreated(logger, realmName);
        return true;
    }

    private static async Task UpdateRealmSettingsAsync(
        IHttpClientFactory httpClientFactory, string token, string keycloakUrl,
        string realmName, string? smtpHost, string? smtpPort, ILogger logger, CancellationToken ct)
    {
        using var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        Dictionary<string, string>? smtpServer = null;

        if (!string.IsNullOrWhiteSpace(smtpHost) && !string.IsNullOrWhiteSpace(smtpPort))
        {
            smtpServer = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["host"] = smtpHost,
                ["port"] = smtpPort,
                ["from"] = "noreply@chairly.local",
                ["fromDisplayName"] = "Chairly",
            };
        }
        else
        {
            LogSmtpConfigMissing(logger);
        }

        var settings = new
        {
            displayName = "Chairly",
            loginTheme = "chairly",
            smtpServer,
        };

        var response = await client.PutAsJsonAsync(
            $"{keycloakUrl}/admin/realms/{realmName}", settings, _jsonOptions, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        LogRealmSettingsUpdated(logger, realmName);
    }

    private static async Task<string?> CreateUserAsync(
        IHttpClientFactory httpClientFactory, string token, string keycloakUrl,
        string realmName, ILogger logger, CancellationToken ct)
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
            $"{keycloakUrl}/admin/realms/{realmName}/users", user, _jsonOptions, ct).ConfigureAwait(false);

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
        string realmName, string userId, CancellationToken ct)
    {
        using var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var credential = new { type = "password", value = DefaultPassword, temporary = false };

        var response = await client.PutAsJsonAsync(
            $"{keycloakUrl}/admin/realms/{realmName}/users/{userId}/reset-password",
            credential, _jsonOptions, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    private static async Task AssignRoleAsync(
        IHttpClientFactory httpClientFactory, string token, string keycloakUrl,
        string realmName, string userId, ILogger logger, CancellationToken ct)
    {
        using var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Get role representation first.
        var roleResponse = await client.GetAsync(
            new Uri($"{keycloakUrl}/admin/realms/{realmName}/roles/{DefaultRole}"), ct).ConfigureAwait(false);
        roleResponse.EnsureSuccessStatusCode();
        var roleJson = await roleResponse.Content.ReadFromJsonAsync<JsonElement>(ct).ConfigureAwait(false);

        // Assign role to user.
        var response = await client.PostAsJsonAsync(
            $"{keycloakUrl}/admin/realms/{realmName}/users/{userId}/role-mappings/realm",
            new[] { roleJson }, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        LogRoleAssigned(logger, DefaultRole, DefaultEmail);
    }

    private static async Task AssignServiceAccountRolesAsync(
        IHttpClientFactory httpClientFactory, string token, string keycloakUrl,
        string realmName, string adminClientId, ILogger logger, CancellationToken ct)
    {
        using var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // 1. Get the realm-management client UUID.
        var realmMgmtResponse = await client.GetAsync(
            new Uri($"{keycloakUrl}/admin/realms/{realmName}/clients?clientId=realm-management"), ct).ConfigureAwait(false);
        realmMgmtResponse.EnsureSuccessStatusCode();
        var realmMgmtClients = await realmMgmtResponse.Content.ReadFromJsonAsync<JsonElement>(ct).ConfigureAwait(false);
        var realmMgmtClientUuid = realmMgmtClients[0].GetProperty("id").GetString()!;

        // 2. Get the chairly-admin client UUID.
        var adminClientResponse = await client.GetAsync(
            new Uri($"{keycloakUrl}/admin/realms/{realmName}/clients?clientId={adminClientId}"), ct).ConfigureAwait(false);
        adminClientResponse.EnsureSuccessStatusCode();
        var adminClients = await adminClientResponse.Content.ReadFromJsonAsync<JsonElement>(ct).ConfigureAwait(false);
        var adminClientUuid = adminClients[0].GetProperty("id").GetString()!;

        // 3. Get the service account user for the admin client.
        var serviceAccountResponse = await client.GetAsync(
            new Uri($"{keycloakUrl}/admin/realms/{realmName}/clients/{adminClientUuid}/service-account-user"), ct).ConfigureAwait(false);
        serviceAccountResponse.EnsureSuccessStatusCode();
        var serviceAccountUser = await serviceAccountResponse.Content.ReadFromJsonAsync<JsonElement>(ct).ConfigureAwait(false);
        var serviceAccountUserId = serviceAccountUser.GetProperty("id").GetString()!;

        // 4. Get available client roles from realm-management and filter for the ones we need.
        var rolesResponse = await client.GetAsync(
            new Uri($"{keycloakUrl}/admin/realms/{realmName}/clients/{realmMgmtClientUuid}/roles"), ct).ConfigureAwait(false);
        rolesResponse.EnsureSuccessStatusCode();
        var allRoles = await rolesResponse.Content.ReadFromJsonAsync<JsonElement>(ct).ConfigureAwait(false);

        var requiredRoleNames = new HashSet<string>(StringComparer.Ordinal) { "manage-users", "manage-realm" };
        var rolesToAssign = new List<JsonElement>();

        foreach (var role in allRoles.EnumerateArray())
        {
            var roleName = role.GetProperty("name").GetString();
            if (roleName is not null && requiredRoleNames.Contains(roleName))
            {
                rolesToAssign.Add(role);
            }
        }

        if (rolesToAssign.Count == 0)
        {
            return;
        }

        // 5. Assign the roles to the service account user.
        var assignResponse = await client.PostAsJsonAsync(
            $"{keycloakUrl}/admin/realms/{realmName}/users/{serviceAccountUserId}/role-mappings/clients/{realmMgmtClientUuid}",
            rolesToAssign, ct).ConfigureAwait(false);

        // 409 Conflict means roles are already assigned — that's fine.
        if (assignResponse.StatusCode != System.Net.HttpStatusCode.Conflict)
        {
            assignResponse.EnsureSuccessStatusCode();
        }

        LogServiceAccountRolesAssigned(logger, adminClientId, realmName);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Keycloak dev seed: created realm {RealmName}")]
    private static partial void LogRealmCreated(ILogger logger, string realmName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Keycloak dev seed: realm {RealmName} already exists, skipping creation")]
    private static partial void LogRealmAlreadyExists(ILogger logger, string realmName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Keycloak dev seed: realm {RealmName} settings updated (displayName=Chairly, loginTheme=chairly)")]
    private static partial void LogRealmSettingsUpdated(ILogger logger, string realmName);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Keycloak dev seed: Smtp:Host or Smtp:Port not configured — skipping SMTP setup for Keycloak realm")]
    private static partial void LogSmtpConfigMissing(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Keycloak dev seed: created user {Email}")]
    private static partial void LogUserCreated(ILogger logger, string email);

    [LoggerMessage(Level = LogLevel.Information, Message = "Keycloak dev seed: user {Email} already exists, skipping setup")]
    private static partial void LogUserAlreadyExists(ILogger logger, string email);

    [LoggerMessage(Level = LogLevel.Information, Message = "Keycloak dev seed: password set for {Email}")]
    private static partial void LogPasswordSet(ILogger logger, string email);

    [LoggerMessage(Level = LogLevel.Information, Message = "Keycloak dev seed: assigned role '{Role}' to {Email}")]
    private static partial void LogRoleAssigned(ILogger logger, string role, string email);

    [LoggerMessage(Level = LogLevel.Information, Message = "Keycloak dev seed: assigned realm-management roles (manage-users, manage-realm) to service account {AdminClientId} in realm {RealmName}")]
    private static partial void LogServiceAccountRolesAssigned(ILogger logger, string adminClientId, string realmName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Keycloak dev seed complete. Login: {Email} / {Password}")]
    private static partial void LogSeedComplete(ILogger logger, string email, string password);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Keycloak admin API not ready (attempt {Attempt}/{MaxAttempts}), retrying...")]
    private static partial void LogKeycloakNotReady(ILogger logger, int attempt, int maxAttempts);

    [LoggerMessage(Level = LogLevel.Error, Message = "Keycloak dev seeder failed — the app will continue without seeded data. You may need to configure Keycloak manually or restart once Keycloak is ready.")]
    internal static partial void LogSeederFailed(ILogger logger, Exception exception);
}
