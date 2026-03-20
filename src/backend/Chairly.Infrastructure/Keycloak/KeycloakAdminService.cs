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

    private string? _cachedToken;
    private DateTimeOffset _tokenExpiry = DateTimeOffset.MinValue;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);

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
        var client = await GetAuthenticatedClientAsync(ct).ConfigureAwait(false);

        var realmRepresentation = new
        {
            realm = tenantId.ToString(),
            enabled = true,
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
        var client = await GetAuthenticatedClientAsync(ct).ConfigureAwait(false);
        var realm = ResolveRealm(tenantId);

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
        var client = await GetAuthenticatedClientAsync(ct).ConfigureAwait(false);
        var realm = ResolveRealm(tenantId);

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
        var client = await GetAuthenticatedClientAsync(ct).ConfigureAwait(false);
        var realm = ResolveRealm(tenantId);

        var response = await client.PutAsJsonAsync(
            $"{_keycloakUrl}/admin/realms/{realm}/users/{keycloakUserId}",
            new { enabled = false },
            _jsonOptions,
            ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    public async Task EnableUserAsync(Guid tenantId, string keycloakUserId, CancellationToken ct = default)
    {
        var client = await GetAuthenticatedClientAsync(ct).ConfigureAwait(false);
        var realm = ResolveRealm(tenantId);

        var response = await client.PutAsJsonAsync(
            $"{_keycloakUrl}/admin/realms/{realm}/users/{keycloakUserId}",
            new { enabled = true },
            _jsonOptions,
            ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteUserAsync(Guid tenantId, string keycloakUserId, CancellationToken ct = default)
    {
        var client = await GetAuthenticatedClientAsync(ct).ConfigureAwait(false);
        var realm = ResolveRealm(tenantId);

        var response = await client.DeleteAsync(
            new Uri($"{_keycloakUrl}/admin/realms/{realm}/users/{keycloakUserId}"),
            ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    public async Task SetPasswordAsync(Guid tenantId, string keycloakUserId, string password,
        bool temporary = false, CancellationToken ct = default)
    {
        var client = await GetAuthenticatedClientAsync(ct).ConfigureAwait(false);
        var realm = ResolveRealm(tenantId);

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

    public async Task DeleteRealmAsync(Guid tenantId, CancellationToken ct = default)
    {
        var client = await GetAuthenticatedClientAsync(ct).ConfigureAwait(false);
        var realm = ResolveRealm(tenantId);

        var response = await client.DeleteAsync(
            new Uri($"{_keycloakUrl}/admin/realms/{realm}"),
            ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    public async Task AssignRealmRoleAsync(Guid tenantId, string keycloakUserId, string roleName,
        CancellationToken ct = default)
    {
        var client = await GetAuthenticatedClientAsync(ct).ConfigureAwait(false);
        var realm = ResolveRealm(tenantId);

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

    internal async Task<HttpClient> GetAuthenticatedClientAsync(CancellationToken ct)
    {
        var token = await GetTokenAsync(ct).ConfigureAwait(false);
        var client = _httpClientFactory.CreateClient("keycloak-admin");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    internal async Task<string> GetTokenAsync(CancellationToken ct)
    {
        await _tokenLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            // Return cached token if still valid (with 30s buffer).
            if (_cachedToken is not null && DateTimeOffset.UtcNow.AddSeconds(30) < _tokenExpiry)
            {
                return _cachedToken;
            }

            var client = _httpClientFactory.CreateClient("keycloak-admin");
            using var tokenRequest = new FormUrlEncodedContent(new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = _adminClientId,
                ["client_secret"] = _adminClientSecret,
            });

            var response = await client.PostAsync(
                new Uri($"{_keycloakUrl}/realms/master/protocol/openid-connect/token"),
                tokenRequest,
                ct).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var tokenResponse = await response.Content.ReadFromJsonAsync<JsonElement>(ct).ConfigureAwait(false);
            _cachedToken = tokenResponse.GetProperty("access_token").GetString()!;
            var expiresIn = tokenResponse.GetProperty("expires_in").GetInt32();
            _tokenExpiry = DateTimeOffset.UtcNow.AddSeconds(expiresIn);

            LogTokenObtained(_logger, expiresIn);

            return _cachedToken;
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    public void Dispose()
    {
        _tokenLock.Dispose();
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Obtained Keycloak admin token, expires in {ExpiresIn}s")]
    private static partial void LogTokenObtained(ILogger logger, int expiresIn);
}
#pragma warning restore CA1812
