using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Tenancy;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Chairly.Api.Features.Billing.GetInvoicesList;

#pragma warning disable CA1812
internal sealed class GetInvoicesListHandler(ChairlyDbContext db) : IRequestHandler<GetInvoicesListQuery, IEnumerable<InvoiceSummaryResponse>>
{
    public async Task<IEnumerable<InvoiceSummaryResponse>> Handle(GetInvoicesListQuery query, CancellationToken cancellationToken = default)
    {
        return await db.Invoices
            .Where(i => i.TenantId == TenantConstants.DefaultTenantId)
            .Join(
                db.Clients,
                i => i.ClientId,
                c => c.Id,
                (i, c) => new { Invoice = i, ClientFullName = c.FirstName + " " + c.LastName })
            .OrderByDescending(x => x.Invoice.CreatedAtUtc)
            .Select(x => new InvoiceSummaryResponse(
                x.Invoice.Id,
                x.Invoice.InvoiceNumber,
                x.Invoice.InvoiceDate,
                x.Invoice.BookingId,
                x.Invoice.ClientId,
                x.ClientFullName,
                x.Invoice.TotalAmount,
                x.Invoice.VoidedAtUtc != null ? "Vervallen" :
                x.Invoice.PaidAtUtc != null ? "Betaald" :
                x.Invoice.SentAtUtc != null ? "Verzonden" : "Concept",
                x.Invoice.CreatedAtUtc,
                x.Invoice.SentAtUtc,
                x.Invoice.PaidAtUtc,
                x.Invoice.VoidedAtUtc))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
#pragma warning restore CA1812
