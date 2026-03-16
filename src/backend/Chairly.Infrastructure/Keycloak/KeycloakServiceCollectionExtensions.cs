using Microsoft.Extensions.DependencyInjection;

namespace Chairly.Infrastructure.Keycloak;

public static class KeycloakServiceCollectionExtensions
{
    public static IServiceCollection AddKeycloakAdmin(this IServiceCollection services)
    {
        services.AddHttpClient("keycloak-admin");
        services.AddSingleton<IKeycloakAdminService, KeycloakAdminService>();
        return services;
    }
}
