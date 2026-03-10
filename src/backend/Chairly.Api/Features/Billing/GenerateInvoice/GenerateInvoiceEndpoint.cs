using Chairly.Api.Shared.Mediator;

namespace Chairly.Api.Features.Billing.GenerateInvoice;

internal static class GenerateInvoiceEndpoint
{
    public static void MapGenerateInvoice(this RouteGroupBuilder group)
    {
        group.MapPost("/", async (
            GenerateInvoiceCommand command,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(command, cancellationToken).ConfigureAwait(false);
            return result.Match(
                invoice => Results.Created($"/api/invoices/{invoice.Id}", invoice),
                _ => Results.NotFound(),
                unprocessable => Results.UnprocessableEntity(new { message = unprocessable.Message }),
                _ => Results.Conflict(new { message = "Er bestaat al een factuur voor deze boeking" }));
        });
    }
}
