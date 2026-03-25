namespace Chairly.Api.Features.Reports;

internal sealed record RevenueReportResponse(
    string PeriodType,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    string SalonName,
    IReadOnlyList<RevenueReportRow> Rows,
    IReadOnlyList<RevenueReportDailyTotal> DailyTotals,
    RevenueReportGrandTotal GrandTotal);
