using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Results;
using Chairly.Api.Shared.Tenancy;
using Chairly.Domain.Entities;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;

#pragma warning disable CA1812
namespace Chairly.Api.Features.Billing.RegenerateInvoice;

internal sealed class RegenerateInvoiceHandler(ChairlyDbContext db, InvoiceLineItemBuilder lineItemBuilder) : IRequestHandler<RegenerateInvoiceCommand, OneOf<InvoiceResponse, NotFound, Unprocessable>>
{
    public async Task<OneOf<InvoiceResponse, NotFound, Unprocessable>> Handle(RegenerateInvoiceCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var invoice = await db.Invoices
            .Include(i => i.LineItems)
            .FirstOrDefaultAsync(i => i.Id == command.Id && i.TenantId == TenantConstants.DefaultTenantId, cancellationToken)
            .ConfigureAwait(false);

        if (invoice is null)
        {
            return new NotFound();
        }

        if (invoice.SentAtUtc != null || invoice.PaidAtUtc != null || invoice.VoidedAtUtc != null)
        {
            return new Unprocessable("Alleen concept-facturen kunnen opnieuw worden gegenereerd");
        }

        var booking = await db.Bookings
            .Include(b => b.BookingServices)
            .FirstOrDefaultAsync(b => b.Id == invoice.BookingId && b.TenantId == TenantConstants.DefaultTenantId, cancellationToken)
            .ConfigureAwait(false);

        if (booking is null)
        {
            return new NotFound();
        }

        if (booking.CompletedAtUtc == null)
        {
            return new Unprocessable("Boeking is niet afgerond");
        }

        await ReplaceLineItemsAsync(invoice, booking, cancellationToken).ConfigureAwait(false);

        var clientFullName = await db.Clients
            .Where(c => c.Id == invoice.ClientId)
            .Select(c => c.FirstName + " " + c.LastName)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false) ?? string.Empty;

        return InvoiceMapper.ToResponse(invoice, clientFullName);
    }

    private async Task ReplaceLineItemsAsync(Invoice invoice, Booking booking, CancellationToken cancellationToken)
    {
        var newLineItems = await lineItemBuilder.BuildFromBookingAsync(booking.BookingServices, cancellationToken).ConfigureAwait(false);

        // Update existing line items in-place and add/remove as needed to avoid
        // EF Core OwnsMany tracking issues with Clear() + re-add patterns.
        var existingItems = invoice.LineItems.ToList();
        for (var i = 0; i < Math.Max(existingItems.Count, newLineItems.Count); i++)
        {
            if (i < existingItems.Count && i < newLineItems.Count)
            {
                // Update existing item in-place
                existingItems[i].Description = newLineItems[i].Description;
                existingItems[i].Quantity = newLineItems[i].Quantity;
                existingItems[i].UnitPrice = newLineItems[i].UnitPrice;
                existingItems[i].TotalPrice = newLineItems[i].TotalPrice;
                existingItems[i].VatPercentage = newLineItems[i].VatPercentage;
                existingItems[i].VatAmount = newLineItems[i].VatAmount;
                existingItems[i].SortOrder = newLineItems[i].SortOrder;
                existingItems[i].IsManual = newLineItems[i].IsManual;
            }
            else if (i >= existingItems.Count)
            {
                // Add new item
                invoice.LineItems.Add(newLineItems[i]);
            }
            else
            {
                // Remove excess old item
                invoice.LineItems.Remove(existingItems[i]);
            }
        }

        InvoiceMapper.RecalculateInvoiceTotals(invoice);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
#pragma warning restore CA1812
