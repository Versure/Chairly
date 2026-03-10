using Chairly.Api.Shared.Mediator;

namespace Chairly.Api.Features.Billing.AddInvoiceLineItem;

internal static class AddInvoiceLineItemEndpoint
{
    public static void MapAddInvoiceLineItem(this RouteGroupBuilder group)
    {
        group.MapPost("/{id:guid}/line-items", async (
            Guid id,
            AddInvoiceLineItemCommand command,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            command.InvoiceId = id;
            var result = await mediator.Send(command, cancellationToken).ConfigureAwait(false);
            return result.Match(
                invoice => Results.Ok(invoice),
                _ => Results.NotFound(),
                unprocessable => Results.UnprocessableEntity(new { message = unprocessable.Message }));
        });
    }
}
