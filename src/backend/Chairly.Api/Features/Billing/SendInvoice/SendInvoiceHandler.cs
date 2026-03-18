using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Results;
using Chairly.Api.Shared.Tenancy;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;

#pragma warning disable CA1812
namespace Chairly.Api.Features.Billing.SendInvoice;

internal sealed class SendInvoiceHandler(ChairlyDbContext db, ITenantContext tenantContext) : IRequestHandler<SendInvoiceCommand, OneOf<InvoiceResponse, NotFound, Unprocessable>>
{
    public async Task<OneOf<InvoiceResponse, NotFound, Unprocessable>> Handle(SendInvoiceCommand command, CancellationToken cancellationToken = default)
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

        if (invoice.VoidedAtUtc != null || invoice.PaidAtUtc != null)
        {
            return new Unprocessable("Betaalde of vervallen facturen kunnen niet worden verstuurd");
        }

        var clientEmail = await db.Clients
            .Where(c => c.Id == invoice.ClientId && c.TenantId == tenantContext.TenantId)
            .Select(c => c.Email)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(clientEmail))
        {
            return new Unprocessable("Cliënt heeft geen e-mailadres");
        }

        // Idempotent: if already sent, return current state
        if (invoice.SentAtUtc == null)
        {
            invoice.SentAtUtc = DateTimeOffset.UtcNow;
            invoice.SentBy = tenantContext.UserId;

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        var (clientFullName, clientSnapshot, staffMemberName) = await InvoiceMapper
            .LoadInvoiceContextAsync(db, invoice, cancellationToken)
            .ConfigureAwait(false);

        return InvoiceMapper.ToResponse(invoice, clientFullName, clientSnapshot, staffMemberName);
    }
}
#pragma warning restore CA1812
