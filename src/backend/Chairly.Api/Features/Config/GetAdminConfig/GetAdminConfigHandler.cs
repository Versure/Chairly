using Chairly.Api.Shared.Mediator;
using Microsoft.Extensions.Configuration;

#pragma warning disable CA1812
namespace Chairly.Api.Features.Config.GetAdminConfig;

internal sealed class GetAdminConfigHandler(IConfiguration configuration) : IRequestHandler<GetAdminConfigQuery, AdminConfigResponse>
{
    public Task<AdminConfigResponse> Handle(GetAdminConfigQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var response = new AdminConfigResponse(
            configuration["Keycloak:Url"] ?? string.Empty,
            configuration["Keycloak:AdminPortalRealm"] ?? "chairly-admin",
            configuration["Keycloak:AdminPortalClientId"] ?? "chairly-admin-portal");

        return Task.FromResult(response);
    }
}
#pragma warning restore CA1812
