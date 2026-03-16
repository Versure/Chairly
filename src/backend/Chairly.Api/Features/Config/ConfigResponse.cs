namespace Chairly.Api.Features.Config;

internal sealed record ConfigResponse(
    string KeycloakUrl,
    string KeycloakRealm,
    string KeycloakClientId);
