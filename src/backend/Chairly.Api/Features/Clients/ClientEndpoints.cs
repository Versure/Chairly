using Chairly.Api.Features.Clients.CreateClient;
using Chairly.Api.Features.Clients.DeleteClient;
using Chairly.Api.Features.Clients.GetClientsList;
using Chairly.Api.Features.Clients.UpdateClient;

namespace Chairly.Api.Features.Clients;

internal static class ClientEndpoints
{
    public static IEndpointRouteBuilder MapClientEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/clients");

        group.MapGetClientsList();
        group.MapCreateClient();
        group.MapUpdateClient();
        group.MapDeleteClient();

        return app;
    }
}
