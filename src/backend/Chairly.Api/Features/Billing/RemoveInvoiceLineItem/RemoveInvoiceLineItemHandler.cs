using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Results;
using Chairly.Api.Shared.Tenancy;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;

#pragma warning disable CA1812
namespace Chairly.Api.Features.Billing.RemoveInvoiceLineItem;

internal sealed class RemoveInvoiceLineItemHandler(ChairlyDbContext db) : IRequestHandler<RemoveInvoiceLineItemCommand, OneOf<InvoiceResponse, NotFound, Unprocessable>>
{
    public async Task<OneOf<InvoiceResponse, NotFound, Unprocessable>> Handle(RemoveInvoiceLineItemCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var invoice = await db.Invoices
            .Include(i => i.LineItems)
            .FirstOrDefaultAsync(i => i.Id == command.InvoiceId && i.TenantId == TenantConstants.DefaultTenantId, cancellationToken)
            .ConfigureAwait(false);

        if (invoice is null)
        {
            return new NotFound();
        }

        // Allow modifications in Draft or Sent state (block if paid or voided)
        if (invoice.PaidAtUtc != null || invoice.VoidedAtUtc != null)
        {
            return new Unprocessable("Betaalde of vervallen facturen kunnen niet worden gewijzigd");
        }

        var lineItem = invoice.LineItems.FirstOrDefault(li => li.Id == command.LineItemId);
        if (lineItem is null)
        {
            return new NotFound();
        }

        if (!lineItem.IsManual)
        {
            return new Unprocessable("Alleen handmatig toegevoegde regels kunnen worden verwijderd");
        }

        invoice.LineItems.Remove(lineItem);

        // If the invoice was already sent, reset to draft so it can be re-sent after editing
        if (invoice.SentAtUtc != null)
        {
            invoice.SentAtUtc = null;
            invoice.SentBy = null;
        }

        InvoiceMapper.RecalculateInvoiceTotals(invoice);

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var clientFullName = await db.Clients
            .Where(c => c.Id == invoice.ClientId)
            .Select(c => c.FirstName + " " + c.LastName)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false) ?? string.Empty;

        return InvoiceMapper.ToResponse(invoice, clientFullName);
    }
}
#pragma warning restore CA1812
