using System.Collections.Concurrent;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

#pragma warning disable CA1812 // Instantiated via DI
namespace Chairly.Api.Shared.Tenancy;

internal sealed class KeycloakJwksCache
{
    private readonly bool _requireHttpsMetadata;
    private readonly ConcurrentDictionary<string, ConfigurationManager<OpenIdConnectConfiguration>> _managers = new(StringComparer.Ordinal);

    public KeycloakJwksCache(bool requireHttpsMetadata = true)
    {
        _requireHttpsMetadata = requireHttpsMetadata;
    }

    public IEnumerable<SecurityKey> GetSigningKeys(string issuer)
    {
        var manager = _managers.GetOrAdd(issuer, iss =>
        {
            var metadataAddress = iss + "/.well-known/openid-configuration";
            return new ConfigurationManager<OpenIdConnectConfiguration>(
                metadataAddress,
                new OpenIdConnectConfigurationRetriever(),
                new HttpDocumentRetriever
                {
                    RequireHttps = _requireHttpsMetadata,
                });
        });

        // GetConfigurationAsync is cached internally and refreshes on key rotation.
        var config = manager.GetConfigurationAsync(CancellationToken.None)
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();

        return config.SigningKeys;
    }
}
#pragma warning restore CA1812
