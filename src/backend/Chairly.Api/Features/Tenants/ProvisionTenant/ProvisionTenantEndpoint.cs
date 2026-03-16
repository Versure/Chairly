using Chairly.Api.Shared.Mediator;

namespace Chairly.Api.Features.Tenants.ProvisionTenant;

internal static class ProvisionTenantEndpoint
{
    public static void MapProvisionTenant(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/tenants", async (
            ProvisionTenantCommand command,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(command, cancellationToken).ConfigureAwait(false);
            return result.Match(
                response => Results.Created($"/api/tenants/{response.TenantId}", response),
                unprocessable => Results.Problem(
                    title: "Keycloak provisioning failed",
                    detail: unprocessable.Message,
                    statusCode: StatusCodes.Status502BadGateway));
        }).AllowAnonymous();
    }
}
