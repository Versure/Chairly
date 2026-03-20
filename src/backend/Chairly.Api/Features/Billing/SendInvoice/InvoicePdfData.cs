namespace Chairly.Api.Features.Billing.SendInvoice;

internal sealed record InvoicePdfData(
    string InvoiceNumber,
    DateOnly InvoiceDate,
    string ClientName,
    string SalonName,
    decimal SubTotalAmount,
    decimal TotalVatAmount,
    decimal TotalAmount,
    bool IsPaid,
    IReadOnlyList<InvoicePdfLineItem> LineItems);
