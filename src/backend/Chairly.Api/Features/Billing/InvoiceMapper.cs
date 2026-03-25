using Chairly.Domain.Entities;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Chairly.Api.Features.Billing;

internal static class InvoiceMapper
{
    public static string DeriveStatus(Invoice invoice)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        if (invoice.VoidedAtUtc != null)
        {
            return "Vervallen";
        }

        if (invoice.PaidAtUtc != null)
        {
            return "Betaald";
        }

        if (invoice.SentAtUtc != null)
        {
            return "Verzonden";
        }

        return "Concept";
    }

    public static async Task<(string ClientFullName, ClientSnapshotResponse ClientSnapshot, string StaffMemberName)>
        LoadInvoiceContextAsync(ChairlyDbContext db, Invoice invoice, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(invoice);

        var client = await db.Clients
            .FirstOrDefaultAsync(c => c.Id == invoice.ClientId, cancellationToken)
            .ConfigureAwait(false);

        var clientFullName = client is not null
            ? client.FirstName + " " + client.LastName
            : string.Empty;

        var clientSnapshot = new ClientSnapshotResponse(
            clientFullName,
            client?.Email,
            client?.PhoneNumber,
            null); // Client entity has no address fields yet

        var staffMemberName = await db.Bookings
            .Where(b => b.Id == invoice.BookingId)
            .Join(db.StaffMembers, b => b.StaffMemberId, s => s.Id, (b, s) => s.FirstName + " " + s.LastName)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false) ?? string.Empty;

        return (clientFullName, clientSnapshot, staffMemberName);
    }

    public static InvoiceResponse ToResponse(Invoice invoice, string clientFullName, ClientSnapshotResponse clientSnapshot, string staffMemberName)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        var lineItems = invoice.LineItems
            .OrderBy(li => li.SortOrder)
            .Select(li => new InvoiceLineItemResponse(
                li.Id,
                li.Description,
                li.Quantity,
                li.UnitPrice,
                li.TotalPrice,
                li.VatPercentage,
                li.VatAmount,
                li.SortOrder,
                li.IsManual))
            .ToList();

        return new InvoiceResponse(
            invoice.Id,
            invoice.InvoiceNumber,
            invoice.InvoiceDate,
            invoice.BookingId,
            invoice.ClientId,
            clientFullName,
            clientSnapshot,
            staffMemberName,
            invoice.SubTotalAmount,
            invoice.TotalVatAmount,
            invoice.TotalAmount,
            DeriveStatus(invoice),
            invoice.PaymentMethod.ToString(),
            lineItems,
            invoice.CreatedAtUtc,
            invoice.SentAtUtc,
            invoice.PaidAtUtc,
            invoice.VoidedAtUtc);
    }

    public static InvoiceSummaryResponse ToSummaryResponse(Invoice invoice, string clientFullName)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        return new InvoiceSummaryResponse(
            invoice.Id,
            invoice.InvoiceNumber,
            invoice.InvoiceDate,
            invoice.BookingId,
            invoice.ClientId,
            clientFullName,
            invoice.SubTotalAmount,
            invoice.TotalVatAmount,
            invoice.TotalAmount,
            DeriveStatus(invoice),
            invoice.PaymentMethod.ToString(),
            invoice.CreatedAtUtc,
            invoice.SentAtUtc,
            invoice.PaidAtUtc,
            invoice.VoidedAtUtc);
    }

    public static void RecalculateInvoiceTotals(Invoice invoice)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        invoice.TotalVatAmount = invoice.LineItems.Sum(li => li.VatAmount);
        invoice.SubTotalAmount = invoice.LineItems.Sum(li => li.TotalPrice) - invoice.TotalVatAmount;
        invoice.TotalAmount = invoice.SubTotalAmount + invoice.TotalVatAmount;
    }
}
