using Chairly.Api.Shared.Mediator;

namespace Chairly.Api.Features.Billing.SendInvoice;

internal static class SendInvoiceEndpoint
{
    public static void MapSendInvoice(this RouteGroupBuilder group)
    {
        group.MapPost("/{id:guid}/send", async (
            Guid id,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new SendInvoiceCommand(id), cancellationToken).ConfigureAwait(false);
            return result.Match(
                invoice => Results.Ok(invoice),
                _ => Results.NotFound(),
                unprocessable => Results.UnprocessableEntity(new { message = unprocessable.Message }));
        });
    }
}
