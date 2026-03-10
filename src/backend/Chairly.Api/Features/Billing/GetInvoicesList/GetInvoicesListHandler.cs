using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Tenancy;
using Chairly.Domain.Entities;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Chairly.Api.Features.Billing.GetInvoicesList;

#pragma warning disable CA1812
internal sealed class GetInvoicesListHandler(ChairlyDbContext db) : IRequestHandler<GetInvoicesListQuery, IEnumerable<InvoiceSummaryResponse>>
{
    public async Task<IEnumerable<InvoiceSummaryResponse>> Handle(GetInvoicesListQuery query, CancellationToken cancellationToken = default)
    {
        var invoiceQuery = ApplyInvoiceFilters(db.Invoices.Where(i => i.TenantId == TenantConstants.DefaultTenantId), query);

        var joinedQuery = invoiceQuery.Join(
            db.Clients, i => i.ClientId, c => c.Id,
            (i, c) => new { Invoice = i, ClientFullName = c.FirstName + " " + c.LastName });

        if (!string.IsNullOrWhiteSpace(query?.ClientName))
        {
            var clientName = query.ClientName.ToUpperInvariant();
#pragma warning disable CA1862 // EF Core cannot translate Contains(string, StringComparison) to SQL
            joinedQuery = joinedQuery.Where(x => x.ClientFullName.ToUpperInvariant().Contains(clientName));
#pragma warning restore CA1862
        }

        return await joinedQuery
            .OrderByDescending(x => x.Invoice.CreatedAtUtc)
            .Select(x => new InvoiceSummaryResponse(
                x.Invoice.Id, x.Invoice.InvoiceNumber, x.Invoice.InvoiceDate,
                x.Invoice.BookingId, x.Invoice.ClientId, x.ClientFullName,
                x.Invoice.SubTotalAmount, x.Invoice.TotalVatAmount, x.Invoice.TotalAmount,
                x.Invoice.VoidedAtUtc != null ? "Vervallen" :
                x.Invoice.PaidAtUtc != null ? "Betaald" :
                x.Invoice.SentAtUtc != null ? "Verzonden" : "Concept",
                x.Invoice.CreatedAtUtc, x.Invoice.SentAtUtc, x.Invoice.PaidAtUtc, x.Invoice.VoidedAtUtc))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    private static IQueryable<Invoice> ApplyInvoiceFilters(IQueryable<Invoice> invoices, GetInvoicesListQuery? query)
    {
        if (query?.ClientId != null)
        {
            invoices = invoices.Where(i => i.ClientId == query.ClientId);
        }

        if (query?.FromDate != null)
        {
            var fromDate = query.FromDate.Value;
            invoices = invoices.Where(i => i.InvoiceDate >= fromDate);
        }

        if (query?.ToDate != null)
        {
            var toDate = query.ToDate.Value;
            invoices = invoices.Where(i => i.InvoiceDate <= toDate);
        }

        if (!string.IsNullOrWhiteSpace(query?.Status))
        {
            invoices = query.Status switch
            {
                "Concept" => invoices.Where(i => i.SentAtUtc == null && i.PaidAtUtc == null && i.VoidedAtUtc == null),
                "Verzonden" => invoices.Where(i => i.SentAtUtc != null && i.PaidAtUtc == null && i.VoidedAtUtc == null),
                "Betaald" => invoices.Where(i => i.PaidAtUtc != null && i.VoidedAtUtc == null),
                "Vervallen" => invoices.Where(i => i.VoidedAtUtc != null),
                _ => invoices,
            };
        }

        return invoices;
    }
}
#pragma warning restore CA1812
