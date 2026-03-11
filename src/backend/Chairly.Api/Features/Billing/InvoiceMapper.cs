using Chairly.Domain.Entities;

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

    public static InvoiceResponse ToResponse(Invoice invoice, string clientFullName)
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
            invoice.SubTotalAmount,
            invoice.TotalVatAmount,
            invoice.TotalAmount,
            DeriveStatus(invoice),
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
