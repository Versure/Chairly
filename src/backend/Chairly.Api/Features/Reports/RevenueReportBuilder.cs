using System.Globalization;
using Chairly.Api.Shared.Results;
using Chairly.Api.Shared.Tenancy;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using OneOf;

namespace Chairly.Api.Features.Reports;

internal static class RevenueReportBuilder
{
    public static OneOf<(DateOnly PeriodStart, DateOnly PeriodEnd), Unprocessable> CalculatePeriod(string period, DateOnly date)
    {
        return period.ToUpperInvariant() switch
        {
            "WEEK" => CalculateWeekPeriod(date),
            "MONTH" => CalculateMonthPeriod(date),
            "YEAR" => CalculateYearPeriod(date),
            _ => new Unprocessable("Ongeldige periode. Gebruik 'week', 'month' of 'year'."),
        };
    }

    public static async Task<RevenueReportResponse> BuildReportAsync(
        ChairlyDbContext db,
        ITenantContext tenantContext,
        string period,
        DateOnly periodStart,
        DateOnly periodEnd,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(tenantContext);

        var invoices = await db.Invoices
            .Include(i => i.LineItems)
            .Where(i => i.TenantId == tenantContext.TenantId
                && i.PaidAtUtc != null
                && i.VoidedAtUtc == null
                && i.InvoiceDate >= periodStart
                && i.InvoiceDate <= periodEnd)
            .OrderBy(i => i.InvoiceDate)
            .ThenBy(i => i.InvoiceNumber)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var salonName = await db.TenantSettings
            .Where(t => t.TenantId == tenantContext.TenantId)
            .Select(t => t.CompanyName)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false) ?? string.Empty;

        var rows = invoices
            .Select(i => new RevenueReportRow(
                i.InvoiceDate,
                i.InvoiceNumber,
                i.TotalAmount,
                i.TotalVatAmount,
                i.PaymentMethod.ToString()))
            .ToList();

        var dailyTotals = rows
            .GroupBy(r => r.Date)
            .Select(g => new RevenueReportDailyTotal(
                g.Key,
                g.Sum(r => r.TotalAmount),
                g.Sum(r => r.VatAmount),
                g.Count()))
            .OrderBy(d => d.Date)
            .ToList();

        var grandTotal = new RevenueReportGrandTotal(
            rows.Sum(r => r.TotalAmount),
            rows.Sum(r => r.VatAmount),
            rows.Count);

        var normalizedPeriod = NormalizePeriod(period);

        return new RevenueReportResponse(
            normalizedPeriod,
            periodStart,
            periodEnd,
            salonName,
            rows,
            dailyTotals,
            grandTotal);
    }

    private static string NormalizePeriod(string period)
    {
        return period.ToUpperInvariant() switch
        {
            "WEEK" => "week",
            "MONTH" => "month",
            "YEAR" => "year",
            _ => period,
        };
    }

    private static (DateOnly PeriodStart, DateOnly PeriodEnd) CalculateWeekPeriod(DateOnly date)
    {
        // ISO Monday
        var dayOfWeek = ((int)date.DayOfWeek + 6) % 7;
        var monday = date.AddDays(-dayOfWeek);
        var sunday = monday.AddDays(6);
        return (monday, sunday);
    }

    private static (DateOnly PeriodStart, DateOnly PeriodEnd) CalculateMonthPeriod(DateOnly date)
    {
        var firstDay = new DateOnly(date.Year, date.Month, 1);
        var lastDay = firstDay.AddMonths(1).AddDays(-1);
        return (firstDay, lastDay);
    }

    private static (DateOnly PeriodStart, DateOnly PeriodEnd) CalculateYearPeriod(DateOnly date)
    {
        var firstDay = new DateOnly(date.Year, 1, 1);
        var lastDay = new DateOnly(date.Year, 12, 31);
        return (firstDay, lastDay);
    }
}
