using Chairly.Api.Shared.Mediator;

namespace Chairly.Api.Features.Billing.RemoveInvoiceLineItem;

internal static class RemoveInvoiceLineItemEndpoint
{
    public static void MapRemoveInvoiceLineItem(this RouteGroupBuilder group)
    {
        group.MapDelete("/{id:guid}/line-items/{lineItemId:guid}", async (
            Guid id,
            Guid lineItemId,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new RemoveInvoiceLineItemCommand(id, lineItemId), cancellationToken).ConfigureAwait(false);
            return result.Match(
                invoice => Results.Ok(invoice),
                _ => Results.NotFound(),
                unprocessable => Results.UnprocessableEntity(new { message = unprocessable.Message }));
        });
    }
}
