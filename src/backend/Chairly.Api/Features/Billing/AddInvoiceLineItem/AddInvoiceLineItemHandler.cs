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

internal sealed class AddInvoiceLineItemHandler(ChairlyDbContext db) : IRequestHandler<AddInvoiceLineItemCommand, OneOf<InvoiceResponse, NotFound, Unprocessable>>
{
    public async Task<OneOf<InvoiceResponse, NotFound, Unprocessable>> Handle(AddInvoiceLineItemCommand command, CancellationToken cancellationToken = default)
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

        // Only allow modifications when invoice is in Draft state
        if (invoice.SentAtUtc != null || invoice.PaidAtUtc != null || invoice.VoidedAtUtc != null)
        {
            return new Unprocessable("Alleen conceptfacturen kunnen worden gewijzigd");
        }

        var totalPrice = command.Quantity * command.UnitPrice;
        var vatAmount = Math.Round(totalPrice * command.VatPercentage / 100m, 2, MidpointRounding.AwayFromZero);

        var maxSortOrder = invoice.LineItems.Count > 0
            ? invoice.LineItems.Max(li => li.SortOrder)
            : -1;

        var lineItem = new InvoiceLineItem
        {
            Id = Guid.NewGuid(),
            Description = command.Description,
            Quantity = command.Quantity,
            UnitPrice = command.UnitPrice,
            TotalPrice = totalPrice,
            VatPercentage = command.VatPercentage,
            VatAmount = vatAmount,
            SortOrder = maxSortOrder + 1,
            IsManual = true,
        };

        invoice.LineItems.Add(lineItem);

        // Explicitly mark the new owned entity as Added for change tracker compatibility
        db.Entry(lineItem).State = EntityState.Added;

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
