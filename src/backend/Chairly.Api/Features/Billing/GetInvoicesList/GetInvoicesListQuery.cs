using Chairly.Api.Shared.Mediator;

namespace Chairly.Api.Features.Billing.GetInvoicesList;

internal sealed class GetInvoicesListQuery : IRequest<IEnumerable<InvoiceSummaryResponse>>
{
}
