namespace Chairly.Api.Features.Billing.SendInvoice;

internal sealed record InvoicePdfLineItem(
    string Description,
    int Quantity,
    decimal UnitPrice,
    decimal VatPercentage,
    decimal LineTotal);
