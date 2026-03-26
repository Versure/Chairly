using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Results;
using Chairly.Api.Shared.Tenancy;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;

#pragma warning disable CA1812
namespace Chairly.Api.Features.Billing.MarkInvoicePaid;

internal sealed class MarkInvoicePaidHandler(ChairlyDbContext db, ITenantContext tenantContext) : IRequestHandler<MarkInvoicePaidCommand, OneOf<InvoiceResponse, NotFound, Unprocessable>>
{
    public async Task<OneOf<InvoiceResponse, NotFound, Unprocessable>> Handle(MarkInvoicePaidCommand command, CancellationToken cancellationToken = default)
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

        if (invoice.VoidedAtUtc != null)
        {
            return new Unprocessable("Vervallen factuur kan niet als betaald worden gemarkeerd");
        }

        // Idempotent: if already paid, return current state
        if (invoice.PaidAtUtc == null)
        {
            invoice.PaidAtUtc = DateTimeOffset.UtcNow;
            invoice.PaidBy = tenantContext.UserId;
            invoice.PaymentMethod = command.PaymentMethod;

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        var (clientFullName, clientSnapshot, staffMemberName) = await InvoiceMapper
            .LoadInvoiceContextAsync(db, invoice, cancellationToken)
            .ConfigureAwait(false);

        return InvoiceMapper.ToResponse(invoice, clientFullName, clientSnapshot, staffMemberName);
    }
}
#pragma warning restore CA1812
