namespace Chairly.Api.Features.Reports;

internal sealed record RevenueReportRow(
    DateOnly Date,
    string InvoiceNumber,
    decimal TotalAmount,
    decimal VatAmount,
    string PaymentMethod);
