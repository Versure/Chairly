namespace Chairly.Api.Features.Config;

internal sealed record AdminConfigResponse(
    string KeycloakUrl,
    string KeycloakRealm,
    string KeycloakClientId);
