using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Chairly.Infrastructure.Keycloak;

#pragma warning disable CA1812 // Instantiated via DI
internal sealed partial class KeycloakAdminService : IKeycloakAdminService, IDisposable
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<KeycloakAdminService> _logger;
    private readonly string _keycloakUrl;
    private readonly string _adminClientId;
    private readonly string _adminClientSecret;
    private readonly string _frontendClientId;
    private readonly string? _configuredRealm;
    private readonly Guid? _configuredTenantId;

    private readonly ConcurrentDictionary<string, (string Token, DateTimeOffset Expiry)> _tokenCache = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _tokenLocks = new(StringComparer.Ordinal);

    public KeycloakAdminService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<KeycloakAdminService> logger)
    {
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(logger);

        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _keycloakUrl = configuration["Keycloak:Url"]!;
        _adminClientId = configuration["Keycloak:AdminClientId"]!;
        _adminClientSecret = configuration["Keycloak:AdminClientSecret"]!;
        _frontendClientId = configuration["Keycloak:ClientId"]!;
        _configuredRealm = configuration["Keycloak:Realm"];
        var configuredTenantId = configuration["Keycloak:TenantId"];
        _configuredTenantId = configuredTenantId is not null && Guid.TryParse(configuredTenantId, out var tenantId)
            ? tenantId
            : null;
    }

    public async Task<string> CreateRealmAsync(Guid tenantId, string adminEmail, CancellationToken ct = default)
    {
        var realm = ResolveRealm(tenantId);
        var client = await GetAuthenticatedClientAsync(realm, ct).ConfigureAwait(false);

        var realmRepresentation = new
        {
            realm = tenantId.ToString(),
            enabled = true,
            resetPasswordAllowed = true,
            clients = new object[]
            {
                new
                {
                    clientId = _frontendClientId,
                    publicClient = true,
                    standardFlowEnabled = true,
                    directAccessGrantsEnabled = false,
                    redirectUris = new[] { "*" },
                    webOrigins = new[] { "*" },
                },
                new
                {
                    clientId = _adminClientId,
                    publicClient = false,
                    serviceAccountsEnabled = true,
                    standardFlowEnabled = false,
                    directAccessGrantsEnabled = false,
                    clientAuthenticatorType = "client-secret",
                    secret = _adminClientSecret,
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

        var response = await client.PostAsJsonAsync($"{_keycloakUrl}/admin/realms", realmRepresentation, _jsonOptions, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        return tenantId.ToString();
    }

    public async Task<string> CreateUserAsync(Guid tenantId, string email, string firstName, string lastName,
        string role, CancellationToken ct = default)
    {
        var realm = ResolveRealm(tenantId);
        var client = await GetAuthenticatedClientAsync(realm, ct).ConfigureAwait(false);

        var userRepresentation = new
        {
            username = email,
            email,
            firstName,
            lastName,
            enabled = true,
            requiredActions = new[] { "UPDATE_PASSWORD" },
        };

        var response = await client.PostAsJsonAsync(
            $"{_keycloakUrl}/admin/realms/{realm}/users",
            userRepresentation,
            _jsonOptions,
            ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        // Extract user ID from Location header: .../users/{userId}
        var location = response.Headers.Location?.ToString()
            ?? throw new InvalidOperationException("Keycloak did not return a Location header for the created user.");

        var userId = location[(location.LastIndexOf('/') + 1)..];
        return userId;
    }

    public async Task UpdateUserAsync(Guid tenantId, string keycloakUserId, string email,
        string firstName, string lastName, CancellationToken ct = default)
    {
        var realm = ResolveRealm(tenantId);
        var client = await GetAuthenticatedClientAsync(realm, ct).ConfigureAwait(false);

        var userRepresentation = new
        {
            email,
            firstName,
            lastName,
            username = email,
        };

        var response = await client.PutAsJsonAsync(
            $"{_keycloakUrl}/admin/realms/{realm}/users/{keycloakUserId}",
            userRepresentation,
            _jsonOptions,
            ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    public async Task DisableUserAsync(Guid tenantId, string keycloakUserId, CancellationToken ct = default)
    {
        var realm = ResolveRealm(tenantId);
        var client = await GetAuthenticatedClientAsync(realm, ct).ConfigureAwait(false);

        var response = await client.PutAsJsonAsync(
            $"{_keycloakUrl}/admin/realms/{realm}/users/{keycloakUserId}",
            new { enabled = false },
            _jsonOptions,
            ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    public async Task EnableUserAsync(Guid tenantId, string keycloakUserId, CancellationToken ct = default)
    {
        var realm = ResolveRealm(tenantId);
        var client = await GetAuthenticatedClientAsync(realm, ct).ConfigureAwait(false);

        var response = await client.PutAsJsonAsync(
            $"{_keycloakUrl}/admin/realms/{realm}/users/{keycloakUserId}",
            new { enabled = true },
            _jsonOptions,
            ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteUserAsync(Guid tenantId, string keycloakUserId, CancellationToken ct = default)
    {
        var realm = ResolveRealm(tenantId);
        var client = await GetAuthenticatedClientAsync(realm, ct).ConfigureAwait(false);

        var response = await client.DeleteAsync(
            new Uri($"{_keycloakUrl}/admin/realms/{realm}/users/{keycloakUserId}"),
            ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    public async Task SetPasswordAsync(Guid tenantId, string keycloakUserId, string password,
        bool temporary = false, CancellationToken ct = default)
    {
        var realm = ResolveRealm(tenantId);
        var client = await GetAuthenticatedClientAsync(realm, ct).ConfigureAwait(false);

        var credential = new
        {
            type = "password",
            value = password,
            temporary,
        };

        var response = await client.PutAsJsonAsync(
            $"{_keycloakUrl}/admin/realms/{realm}/users/{keycloakUserId}/reset-password",
            credential,
            _jsonOptions,
            ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    public async Task SendActionsEmailAsync(Guid tenantId, string keycloakUserId, string[] actions,
        CancellationToken ct = default)
    {
        var realm = ResolveRealm(tenantId);
        var client = await GetAuthenticatedClientAsync(realm, ct).ConfigureAwait(false);

        var response = await client.PutAsJsonAsync(
            $"{_keycloakUrl}/admin/realms/{realm}/users/{keycloakUserId}/execute-actions-email",
            actions,
            ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteRealmAsync(Guid tenantId, CancellationToken ct = default)
    {
        var realm = ResolveRealm(tenantId);
        var client = await GetAuthenticatedClientAsync(realm, ct).ConfigureAwait(false);

        var response = await client.DeleteAsync(
            new Uri($"{_keycloakUrl}/admin/realms/{realm}"),
            ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    public async Task AssignRealmRoleAsync(Guid tenantId, string keycloakUserId, string roleName,
        CancellationToken ct = default)
    {
        var realm = ResolveRealm(tenantId);
        var client = await GetAuthenticatedClientAsync(realm, ct).ConfigureAwait(false);

        // First get the role representation.
        var roleResponse = await client.GetAsync(
            new Uri($"{_keycloakUrl}/admin/realms/{realm}/roles/{roleName}"),
            ct).ConfigureAwait(false);
        roleResponse.EnsureSuccessStatusCode();

        var roleJson = await roleResponse.Content.ReadFromJsonAsync<JsonElement>(ct).ConfigureAwait(false);

        // Assign the role to the user.
        var response = await client.PostAsJsonAsync(
            $"{_keycloakUrl}/admin/realms/{realm}/users/{keycloakUserId}/role-mappings/realm",
            new[] { roleJson },
            ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    public async Task<string?> GetUserDisplayNameAsync(string realmName, string userId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(realmName);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        try
        {
            var client = await GetAuthenticatedClientAsync(realmName, ct).ConfigureAwait(false);
            var response = await client.GetAsync(
                new Uri($"{_keycloakUrl}/admin/realms/{realmName}/users/{userId}"),
                ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                LogUserLookupFailed(_logger, userId, realmName, (int)response.StatusCode);
                return null;
            }

            var user = await response.Content.ReadFromJsonAsync<JsonElement>(ct).ConfigureAwait(false);
            var firstName = user.TryGetProperty("firstName", out var fn) ? fn.GetString() : null;
            var lastName = user.TryGetProperty("lastName", out var ln) ? ln.GetString() : null;

            return string.IsNullOrWhiteSpace(firstName) && string.IsNullOrWhiteSpace(lastName)
                ? user.TryGetProperty("username", out var un) ? un.GetString() : null
                : $"{firstName} {lastName}".Trim();
        }
        catch (HttpRequestException ex)
        {
            LogUserLookupException(_logger, userId, realmName, ex);
            return null;
        }
    }

    private string ResolveRealm(Guid tenantId)
    {
        if (_configuredTenantId.HasValue
            && _configuredTenantId.Value == tenantId
            && !string.IsNullOrWhiteSpace(_configuredRealm))
        {
            return _configuredRealm;
        }

        return tenantId.ToString();
    }

    internal async Task<HttpClient> GetAuthenticatedClientAsync(string realm, CancellationToken ct)
    {
        var token = await GetTokenAsync(realm, ct).ConfigureAwait(false);
        var client = _httpClientFactory.CreateClient("keycloak-admin");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    internal async Task<string> GetTokenAsync(string realm, CancellationToken ct)
    {
        var realmLock = _tokenLocks.GetOrAdd(realm, static _ => new SemaphoreSlim(1, 1));
        await realmLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            // Return cached token if still valid (with 30s buffer).
            if (_tokenCache.TryGetValue(realm, out var cached) && DateTimeOffset.UtcNow.AddSeconds(30) < cached.Expiry)
            {
                return cached.Token;
            }

            var client = _httpClientFactory.CreateClient("keycloak-admin");
            using var tokenRequest = new FormUrlEncodedContent(new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = _adminClientId,
                ["client_secret"] = _adminClientSecret,
            });

            var response = await client.PostAsync(
                new Uri($"{_keycloakUrl}/realms/{realm}/protocol/openid-connect/token"),
                tokenRequest,
                ct).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var tokenResponse = await response.Content.ReadFromJsonAsync<JsonElement>(ct).ConfigureAwait(false);
            var token = tokenResponse.GetProperty("access_token").GetString()!;
            var expiresIn = tokenResponse.GetProperty("expires_in").GetInt32();
            _tokenCache[realm] = (token, DateTimeOffset.UtcNow.AddSeconds(expiresIn));

            LogTokenObtained(_logger, realm, expiresIn);

            return token;
        }
        finally
        {
            realmLock.Release();
        }
    }

    public void Dispose()
    {
        foreach (var semaphore in _tokenLocks.Values)
        {
            semaphore.Dispose();
        }
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Obtained Keycloak admin token for realm {Realm}, expires in {ExpiresIn}s")]
    private static partial void LogTokenObtained(ILogger logger, string realm, int expiresIn);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to look up user {UserId} in realm {Realm}: HTTP {StatusCode}")]
    private static partial void LogUserLookupFailed(ILogger logger, string userId, string realm, int statusCode);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Exception looking up user {UserId} in realm {Realm}")]
    private static partial void LogUserLookupException(ILogger logger, string userId, string realm, Exception ex);
}
#pragma warning restore CA1812
