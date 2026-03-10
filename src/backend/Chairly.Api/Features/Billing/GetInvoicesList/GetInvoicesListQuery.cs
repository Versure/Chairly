using Chairly.Api.Shared.Mediator;

namespace Chairly.Api.Features.Billing.GetInvoicesList;

internal sealed class GetInvoicesListQuery : IRequest<IEnumerable<InvoiceSummaryResponse>>
{
    public string? ClientName { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public string? Status { get; set; }
    public Guid? ClientId { get; set; }
}
