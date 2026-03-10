using Chairly.Api.Shared.Mediator;

namespace Chairly.Api.Features.Billing.GetInvoicesList;

internal static class GetInvoicesListEndpoint
{
    public static void MapGetInvoicesList(this RouteGroupBuilder group)
    {
        group.MapGet("/", async (
            string? clientName,
            DateOnly? fromDate,
            DateOnly? toDate,
            string? status,
            Guid? clientId,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var query = new GetInvoicesListQuery
            {
                ClientName = clientName,
                FromDate = fromDate,
                ToDate = toDate,
                Status = status,
                ClientId = clientId,
            };
            var result = await mediator.Send(query, cancellationToken).ConfigureAwait(false);
            return Results.Ok(result);
        });
    }
}
