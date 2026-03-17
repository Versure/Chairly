using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Tenancy;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;

#pragma warning disable CA1812
namespace Chairly.Api.Features.Billing.GetInvoice;

internal sealed class GetInvoiceHandler(ChairlyDbContext db, ITenantContext tenantContext) : IRequestHandler<GetInvoiceQuery, OneOf<InvoiceResponse, NotFound>>
{
    public async Task<OneOf<InvoiceResponse, NotFound>> Handle(GetInvoiceQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var invoice = await db.Invoices
            .Include(i => i.LineItems)
            .FirstOrDefaultAsync(i => i.Id == query.Id && i.TenantId == tenantContext.TenantId, cancellationToken)
            .ConfigureAwait(false);

        if (invoice is null)
        {
            return new NotFound();
        }

        var (clientFullName, clientSnapshot, staffMemberName) = await InvoiceMapper
            .LoadInvoiceContextAsync(db, invoice, cancellationToken)
            .ConfigureAwait(false);

        return InvoiceMapper.ToResponse(invoice, clientFullName, clientSnapshot, staffMemberName);
    }
}
#pragma warning restore CA1812
