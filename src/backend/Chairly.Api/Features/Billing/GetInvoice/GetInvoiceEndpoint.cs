using Chairly.Api.Shared.Mediator;

namespace Chairly.Api.Features.Billing.GetInvoice;

internal static class GetInvoiceEndpoint
{
    public static void MapGetInvoice(this RouteGroupBuilder group)
    {
        group.MapGet("/{id:guid}", async (
            Guid id,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new GetInvoiceQuery(id), cancellationToken).ConfigureAwait(false);
            return result.Match(
                invoice => Results.Ok(invoice),
                _ => Results.NotFound());
        });
    }
}
