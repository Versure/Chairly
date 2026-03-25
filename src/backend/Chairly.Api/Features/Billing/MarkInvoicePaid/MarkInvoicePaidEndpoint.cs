using Chairly.Api.Shared.Mediator;

namespace Chairly.Api.Features.Billing.MarkInvoicePaid;

internal static class MarkInvoicePaidEndpoint
{
    public static void MapMarkInvoicePaid(this RouteGroupBuilder group)
    {
        group.MapPost("/{id:guid}/pay", async (
            Guid id,
            MarkInvoicePaidRequest request,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(
                new MarkInvoicePaidCommand(id, request.PaymentMethod),
                cancellationToken).ConfigureAwait(false);
            return result.Match(
                invoice => Results.Ok(invoice),
                _ => Results.NotFound(),
                unprocessable => Results.UnprocessableEntity(new { message = unprocessable.Message }));
        });
    }
}
