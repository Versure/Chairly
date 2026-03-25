namespace Chairly.Api.Features.Reports;

internal sealed record RevenueReportDailyTotal(
    DateOnly Date,
    decimal TotalAmount,
    decimal VatAmount,
    int InvoiceCount);
