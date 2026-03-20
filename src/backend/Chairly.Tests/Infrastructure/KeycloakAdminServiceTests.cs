using System.Net;
using System.Text.Json;
using Chairly.Infrastructure.Keycloak;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Chairly.Tests.Infrastructure;

public class KeycloakAdminServiceTests : IDisposable
{
    private const string KeycloakUrl = "http://localhost:8080";
    private const string AdminClientId = "chairly-admin";
    private const string AdminClientSecret = "test-secret";
    private const string FrontendClientId = "chairly-frontend";

    private readonly List<IDisposable> _disposables = [];

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (var disposable in _disposables)
            {
                disposable.Dispose();
            }
        }
    }

    private static IConfiguration CreateConfiguration(
        string? realm = null,
        Guid? tenantId = null)
    {
        var values = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["Keycloak:Url"] = KeycloakUrl,
            ["Keycloak:AdminClientId"] = AdminClientId,
            ["Keycloak:AdminClientSecret"] = AdminClientSecret,
            ["Keycloak:ClientId"] = FrontendClientId,
        };

        if (!string.IsNullOrWhiteSpace(realm))
        {
            values["Keycloak:Realm"] = realm;
        }

        if (tenantId.HasValue)
        {
            values["Keycloak:TenantId"] = tenantId.Value.ToString();
        }

        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }

    private static string CreateTokenResponseJson(int expiresIn = 300)
    {
        return JsonSerializer.Serialize(new
        {
            access_token = "test-access-token",
            expires_in = expiresIn,
            token_type = "Bearer",
        });
    }

    [Fact]
    public async Task CreateUserAsync_SendsCorrectBodyAndParsesLocationHeader()
    {
        var tenantId = Guid.NewGuid();
        var expectedUserId = Guid.NewGuid().ToString();
        string? capturedRequestBody = null;

        using var handler = new DelegateHttpMessageHandler(async (request, ct) =>
        {
            var uri = request.RequestUri!.ToString();

            // Token request
            if (uri.Contains("/protocol/openid-connect/token", StringComparison.Ordinal))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(CreateTokenResponseJson(), System.Text.Encoding.UTF8, "application/json"),
                };
            }

            // Create user request
            if (uri.Contains($"/admin/realms/{tenantId}/users", StringComparison.Ordinal) && request.Method == HttpMethod.Post)
            {
                capturedRequestBody = await request.Content!.ReadAsStringAsync(ct).ConfigureAwait(false);

                var response = new HttpResponseMessage(HttpStatusCode.Created);
                response.Headers.Location = new Uri($"{KeycloakUrl}/admin/realms/{tenantId}/users/{expectedUserId}");
                return response;
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        using var service = CreateService(handler);

        var userId = await service.CreateUserAsync(tenantId, "test@example.com", "Jan", "Jansen", "owner");

        Assert.Equal(expectedUserId, userId);
        Assert.NotNull(capturedRequestBody);

        using var bodyDoc = JsonDocument.Parse(capturedRequestBody);
        Assert.Equal("test@example.com", bodyDoc.RootElement.GetProperty("username").GetString());
        Assert.Equal("test@example.com", bodyDoc.RootElement.GetProperty("email").GetString());
        Assert.Equal("Jan", bodyDoc.RootElement.GetProperty("firstName").GetString());
        Assert.Equal("Jansen", bodyDoc.RootElement.GetProperty("lastName").GetString());
        Assert.True(bodyDoc.RootElement.GetProperty("enabled").GetBoolean());
    }

    [Fact]
    public async Task GetTokenAsync_CachesTokenWithinLifetime()
    {
        var tokenRequestCount = 0;

        using var handler = new DelegateHttpMessageHandler((request, _) =>
        {
            var uri = request.RequestUri!.ToString();

            if (uri.Contains("/protocol/openid-connect/token", StringComparison.Ordinal))
            {
                tokenRequestCount++;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(CreateTokenResponseJson(expiresIn: 300), System.Text.Encoding.UTF8, "application/json"),
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });

        using var service = CreateService(handler);

        // First call - should fetch token
        await service.GetTokenAsync(CancellationToken.None);
        Assert.Equal(1, tokenRequestCount);

        // Second call - should use cached token
        await service.GetTokenAsync(CancellationToken.None);
        Assert.Equal(1, tokenRequestCount);
    }

    [Fact]
    public async Task GetTokenAsync_RefreshesTokenNearExpiry()
    {
        var tokenRequestCount = 0;

        using var handler = new DelegateHttpMessageHandler((request, _) =>
        {
            var uri = request.RequestUri!.ToString();

            if (uri.Contains("/protocol/openid-connect/token", StringComparison.Ordinal))
            {
                tokenRequestCount++;
                // Token expires in 10 seconds - within the 30s buffer, so it should be refreshed on next call
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(CreateTokenResponseJson(expiresIn: 10), System.Text.Encoding.UTF8, "application/json"),
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });

        using var service = CreateService(handler);

        // First call - should fetch token
        await service.GetTokenAsync(CancellationToken.None);
        Assert.Equal(1, tokenRequestCount);

        // Second call - token is within 30s of expiry (10s < 30s), should refresh
        await service.GetTokenAsync(CancellationToken.None);
        Assert.Equal(2, tokenRequestCount);
    }

    [Fact]
    public async Task CreateUserAsync_UsesConfiguredRealmWhenTenantMatches()
    {
        var tenantId = Guid.NewGuid();
        const string configuredRealm = "chairly";
        var expectedUserId = Guid.NewGuid().ToString();
        var capturedUris = new List<string>();

        using var handler = new DelegateHttpMessageHandler(async (request, _) =>
        {
            var uri = request.RequestUri!.ToString();
            capturedUris.Add(uri);

            if (uri.Contains("/protocol/openid-connect/token", StringComparison.Ordinal))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(CreateTokenResponseJson(), System.Text.Encoding.UTF8, "application/json"),
                };
            }

            if (uri.Contains($"/admin/realms/{configuredRealm}/users", StringComparison.Ordinal)
                && request.Method == HttpMethod.Post)
            {
                var response = new HttpResponseMessage(HttpStatusCode.Created);
                response.Headers.Location = new Uri($"{KeycloakUrl}/admin/realms/{configuredRealm}/users/{expectedUserId}");
                await Task.CompletedTask.ConfigureAwait(false);
                return response;
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        using var service = CreateService(handler, CreateConfiguration(configuredRealm, tenantId));

        var userId = await service.CreateUserAsync(tenantId, "test@example.com", "Jan", "Jansen", "owner");

        Assert.Equal(expectedUserId, userId);
        Assert.Contains(capturedUris, uri => uri.Contains($"/admin/realms/{configuredRealm}/users", StringComparison.Ordinal));
        Assert.DoesNotContain(capturedUris, uri => uri.Contains($"/admin/realms/{tenantId}/users", StringComparison.Ordinal));
    }

    private KeycloakAdminService CreateService(DelegateHttpMessageHandler handler, IConfiguration? configuration = null)
    {
        var httpClient = new HttpClient(handler, disposeHandler: false);
        _disposables.Add(httpClient);
        var factory = new SingleClientFactory(httpClient);

        return new KeycloakAdminService(
            factory,
            configuration ?? CreateConfiguration(),
            NullLogger<KeycloakAdminService>.Instance);
    }

    /// <summary>
    /// A simple HttpMessageHandler that delegates to a provided function.
    /// </summary>
    private sealed class DelegateHttpMessageHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handlerFunc) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => handlerFunc(request, cancellationToken);
    }

    /// <summary>
    /// A simple IHttpClientFactory that always returns the same HttpClient instance.
    /// The returned client does NOT get disposed by the factory consumer.
    /// </summary>
    private sealed class SingleClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }
}
