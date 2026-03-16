using Chairly.Api.Shared.Mediator;
using Microsoft.Extensions.Configuration;

namespace Chairly.Api.Features.Config.GetConfig;

#pragma warning disable CA1812
internal sealed class GetConfigHandler(IConfiguration configuration) : IRequestHandler<GetConfigQuery, ConfigResponse>
{
    public Task<ConfigResponse> Handle(GetConfigQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var response = new ConfigResponse(
            configuration["Keycloak:Url"]!,
            configuration["Keycloak:Realm"]!,
            configuration["Keycloak:ClientId"]!);

        return Task.FromResult(response);
    }
}
#pragma warning restore CA1812
