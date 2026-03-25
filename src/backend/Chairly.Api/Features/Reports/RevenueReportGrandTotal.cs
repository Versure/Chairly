namespace Chairly.Api.Features.Reports;

internal sealed record RevenueReportGrandTotal(
    decimal TotalAmount,
    decimal VatAmount,
    int InvoiceCount);
