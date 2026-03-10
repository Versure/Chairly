using Chairly.Api.Shared.Mediator;

namespace Chairly.Api.Features.Billing.MarkInvoiceSent;

internal static class MarkInvoiceSentEndpoint
{
    public static void MapMarkInvoiceSent(this RouteGroupBuilder group)
    {
        group.MapPost("/{id:guid}/send", async (
            Guid id,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new MarkInvoiceSentCommand(id), cancellationToken).ConfigureAwait(false);
            return result.Match(
                invoice => Results.Ok(invoice),
                _ => Results.NotFound(),
                unprocessable => Results.UnprocessableEntity(new { message = unprocessable.Message }));
        });
    }
}
