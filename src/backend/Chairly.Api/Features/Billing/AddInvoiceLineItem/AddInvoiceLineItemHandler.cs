using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Results;
using Chairly.Api.Shared.Tenancy;
using Chairly.Domain.Entities;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;

#pragma warning disable CA1812
namespace Chairly.Api.Features.Billing.AddInvoiceLineItem;

internal sealed class AddInvoiceLineItemHandler(ChairlyDbContext db, ITenantContext tenantContext) : IRequestHandler<AddInvoiceLineItemCommand, OneOf<InvoiceResponse, NotFound, Unprocessable>>
{
    public async Task<OneOf<InvoiceResponse, NotFound, Unprocessable>> Handle(AddInvoiceLineItemCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var invoice = await db.Invoices
            .Include(i => i.LineItems)
            .FirstOrDefaultAsync(i => i.Id == command.InvoiceId && i.TenantId == tenantContext.TenantId, cancellationToken)
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

        var lineItem = CreateLineItem(command, invoice);
        invoice.LineItems.Add(lineItem);
        db.Entry(lineItem).State = EntityState.Added;

        ResetSentStateIfNeeded(invoice);
        InvoiceMapper.RecalculateInvoiceTotals(invoice);

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var (clientFullName, clientSnapshot, staffMemberName) = await InvoiceMapper
            .LoadInvoiceContextAsync(db, invoice, cancellationToken)
            .ConfigureAwait(false);

        return InvoiceMapper.ToResponse(invoice, clientFullName, clientSnapshot, staffMemberName);
    }

    private static InvoiceLineItem CreateLineItem(AddInvoiceLineItemCommand command, Invoice invoice)
    {
        var totalPrice = command.Quantity * command.UnitPrice;
        var isDiscount = command.UnitPrice < 0;
        var vatPercentage = isDiscount ? 0m : command.VatPercentage;
        var vatAmount = isDiscount ? 0m : Math.Round(totalPrice * command.VatPercentage / 100m, 2, MidpointRounding.AwayFromZero);
        var maxSortOrder = invoice.LineItems.Count > 0 ? invoice.LineItems.Max(li => li.SortOrder) : -1;

        return new InvoiceLineItem
        {
            Id = Guid.NewGuid(),
            Description = command.Description,
            Quantity = command.Quantity,
            UnitPrice = command.UnitPrice,
            TotalPrice = totalPrice,
            VatPercentage = vatPercentage,
            VatAmount = vatAmount,
            SortOrder = maxSortOrder + 1,
            IsManual = true,
        };
    }

    private static void ResetSentStateIfNeeded(Invoice invoice)
    {
        if (invoice.SentAtUtc != null)
        {
            invoice.SentAtUtc = null;
            invoice.SentBy = null;
        }
    }
}
#pragma warning restore CA1812
