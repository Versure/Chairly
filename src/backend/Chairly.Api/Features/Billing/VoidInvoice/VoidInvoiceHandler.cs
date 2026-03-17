using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Results;
using Chairly.Api.Shared.Tenancy;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;

#pragma warning disable CA1812
namespace Chairly.Api.Features.Billing.VoidInvoice;

internal sealed class VoidInvoiceHandler(ChairlyDbContext db, ITenantContext tenantContext) : IRequestHandler<VoidInvoiceCommand, OneOf<InvoiceResponse, NotFound, Unprocessable>>
{
    public async Task<OneOf<InvoiceResponse, NotFound, Unprocessable>> Handle(VoidInvoiceCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var invoice = await db.Invoices
            .Include(i => i.LineItems)
            .FirstOrDefaultAsync(i => i.Id == command.Id && i.TenantId == tenantContext.TenantId, cancellationToken)
            .ConfigureAwait(false);

        if (invoice is null)
        {
            return new NotFound();
        }

        if (invoice.PaidAtUtc != null)
        {
            return new Unprocessable("Betaalde factuur kan niet vervallen worden verklaard");
        }

        // Not idempotent per spec, but only set if not already voided
        if (invoice.VoidedAtUtc == null)
        {
            invoice.VoidedAtUtc = DateTimeOffset.UtcNow;
            invoice.VoidedBy = tenantContext.UserId;

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        var (clientFullName, clientSnapshot, staffMemberName) = await InvoiceMapper
            .LoadInvoiceContextAsync(db, invoice, cancellationToken)
            .ConfigureAwait(false);

        return InvoiceMapper.ToResponse(invoice, clientFullName, clientSnapshot, staffMemberName);
    }
}
#pragma warning restore CA1812
