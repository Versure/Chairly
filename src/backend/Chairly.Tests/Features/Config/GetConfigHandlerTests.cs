using Chairly.Api.Features.Config.GetConfig;
using Microsoft.Extensions.Configuration;

namespace Chairly.Tests.Features.Config;

public class GetConfigHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsValuesFromConfiguration()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["Keycloak:Url"] = "http://localhost:8080",
                ["Keycloak:Realm"] = "00000000-0000-0000-0000-000000000001",
                ["Keycloak:ClientId"] = "chairly-frontend",
            })
            .Build();

        var handler = new GetConfigHandler(configuration);
        var result = await handler.Handle(new GetConfigQuery());

        Assert.Equal("http://localhost:8080", result.KeycloakUrl);
        Assert.Equal("00000000-0000-0000-0000-000000000001", result.KeycloakRealm);
        Assert.Equal("chairly-frontend", result.KeycloakClientId);
    }
}
