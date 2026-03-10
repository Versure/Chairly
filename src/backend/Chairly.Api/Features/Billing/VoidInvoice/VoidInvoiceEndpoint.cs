using Chairly.Api.Shared.Mediator;

namespace Chairly.Api.Features.Billing.VoidInvoice;

internal static class VoidInvoiceEndpoint
{
    public static void MapVoidInvoice(this RouteGroupBuilder group)
    {
        group.MapPost("/{id:guid}/void", async (
            Guid id,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new VoidInvoiceCommand(id), cancellationToken).ConfigureAwait(false);
            return result.Match(
                invoice => Results.Ok(invoice),
                _ => Results.NotFound(),
                unprocessable => Results.UnprocessableEntity(new { message = unprocessable.Message }));
        });
    }
}
