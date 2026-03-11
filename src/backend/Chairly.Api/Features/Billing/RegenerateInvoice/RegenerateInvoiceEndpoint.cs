using Chairly.Api.Shared.Mediator;

namespace Chairly.Api.Features.Billing.RegenerateInvoice;

internal static class RegenerateInvoiceEndpoint
{
    public static void MapRegenerateInvoice(this RouteGroupBuilder group)
    {
        group.MapPost("/{id:guid}/regenerate", async (
            Guid id,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new RegenerateInvoiceCommand(id), cancellationToken).ConfigureAwait(false);
            return result.Match(
                invoice => Results.Ok(invoice),
                _ => Results.NotFound(),
                unprocessable => Results.UnprocessableEntity(new { message = unprocessable.Message }));
        });
    }
}
