using Chairly.Api.Shared.Mediator;

namespace Chairly.Api.Features.Billing.GetInvoicesList;

internal static class GetInvoicesListEndpoint
{
    public static void MapGetInvoicesList(this RouteGroupBuilder group)
    {
        group.MapGet("/", async (
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new GetInvoicesListQuery(), cancellationToken).ConfigureAwait(false);
            return Results.Ok(result);
        });
    }
}
