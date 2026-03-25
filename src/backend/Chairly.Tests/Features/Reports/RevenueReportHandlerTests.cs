using Chairly.Api.Features.Reports;
using Chairly.Api.Features.Reports.GetRevenueReport;
using Chairly.Api.Features.Reports.GetRevenueReportPdf;
using Chairly.Domain.Entities;
using Chairly.Domain.Enums;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Chairly.Tests.Features.Reports;

public class RevenueReportHandlerTests
{
    static RevenueReportHandlerTests()
    {
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
    }

    private static ChairlyDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ChairlyDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ChairlyDbContext(options);
    }

    private static Invoice CreatePaidInvoice(
        ChairlyDbContext db,
        DateOnly invoiceDate,
        string invoiceNumber,
        decimal totalAmount,
        decimal vatAmount,
        PaymentMethod paymentMethod = PaymentMethod.Pin)
    {
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            TenantId = TestTenantContext.DefaultTenantId,
            BookingId = Guid.NewGuid(),
            ClientId = Guid.NewGuid(),
            InvoiceNumber = invoiceNumber,
            InvoiceDate = invoiceDate,
            SubTotalAmount = totalAmount - vatAmount,
            TotalVatAmount = vatAmount,
            TotalAmount = totalAmount,
            PaymentMethod = paymentMethod,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            PaidAtUtc = DateTimeOffset.UtcNow,
#pragma warning disable MA0026
            PaidBy = Guid.Empty,
#pragma warning restore MA0026
            LineItems =
            [
                new InvoiceLineItem
                {
                    Id = Guid.NewGuid(),
                    Description = "Herenknippen",
                    Quantity = 1,
                    UnitPrice = totalAmount,
                    TotalPrice = totalAmount,
                    VatPercentage = 21.00m,
                    VatAmount = vatAmount,
                    SortOrder = 0,
                    IsManual = false,
                },
            ],
        };

        // Create a booking for the invoice
        var booking = new Booking
        {
            Id = invoice.BookingId,
            TenantId = TestTenantContext.DefaultTenantId,
            ClientId = invoice.ClientId,
            StaffMemberId = Guid.NewGuid(),
            StartTime = DateTimeOffset.UtcNow.AddHours(-2),
            EndTime = DateTimeOffset.UtcNow.AddHours(-1),
            CreatedAtUtc = DateTimeOffset.UtcNow.AddDays(-1),
        };
        db.Bookings.Add(booking);

        db.Invoices.Add(invoice);
        db.SaveChanges();
        return invoice;
    }

    private static void CreateTenantSettings(ChairlyDbContext db, string companyName)
    {
        db.TenantSettings.Add(new TenantSettings
        {
            Id = Guid.NewGuid(),
            TenantId = TestTenantContext.DefaultTenantId,
            CompanyName = companyName,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        });
        db.SaveChanges();
    }

    // ── GetRevenueReport ─────────────────────────────────────────────

    [Fact]
    public async Task GetRevenueReportHandler_WeekPeriod_ReturnsCorrectBoundaries()
    {
        await using var db = CreateDbContext();
        var tenantContext = TestTenantContext.Create();
        CreateTenantSettings(db, "Salon Test");

        // Wednesday 2026-03-25 → week should be Mon 2026-03-23 to Sun 2026-03-29
        var handler = new GetRevenueReportHandler(db, tenantContext);
        var result = await handler.Handle(new GetRevenueReportQuery("week", new DateOnly(2026, 3, 25)));

        var report = result.AsT0;
        Assert.Equal(new DateOnly(2026, 3, 23), report.PeriodStart);
        Assert.Equal(new DateOnly(2026, 3, 29), report.PeriodEnd);
        Assert.Equal("week", report.PeriodType);
    }

    [Fact]
    public async Task GetRevenueReportHandler_MonthPeriod_ReturnsCorrectBoundaries()
    {
        await using var db = CreateDbContext();
        var tenantContext = TestTenantContext.Create();
        CreateTenantSettings(db, "Salon Test");

        var handler = new GetRevenueReportHandler(db, tenantContext);
        var result = await handler.Handle(new GetRevenueReportQuery("month", new DateOnly(2026, 3, 15)));

        var report = result.AsT0;
        Assert.Equal(new DateOnly(2026, 3, 1), report.PeriodStart);
        Assert.Equal(new DateOnly(2026, 3, 31), report.PeriodEnd);
        Assert.Equal("month", report.PeriodType);
    }

    [Fact]
    public async Task GetRevenueReportHandler_InvalidPeriod_ReturnsUnprocessable()
    {
        await using var db = CreateDbContext();
        var tenantContext = TestTenantContext.Create();

        var handler = new GetRevenueReportHandler(db, tenantContext);
        var result = await handler.Handle(new GetRevenueReportQuery("quarter", new DateOnly(2026, 3, 25)));

        Assert.True(result.IsT1);
        Assert.Equal("Ongeldige periode. Gebruik 'week' of 'month'.", result.AsT1.Message);
    }

    [Fact]
    public async Task GetRevenueReportHandler_ReturnsPaidNonVoidedInvoicesOnly()
    {
        await using var db = CreateDbContext();
        var tenantContext = TestTenantContext.Create();
        CreateTenantSettings(db, "Salon Test");

        // Create paid invoice within period
        CreatePaidInvoice(db, new DateOnly(2026, 3, 23), "2026-0001", 65.00m, 11.30m);

        // Create voided invoice within period (should be excluded)
        var voidedInvoice = CreatePaidInvoice(db, new DateOnly(2026, 3, 24), "2026-0002", 45.00m, 7.82m);
        voidedInvoice.VoidedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();

        // Create unpaid invoice within period (should be excluded)
        var unpaidInvoice = new Invoice
        {
            Id = Guid.NewGuid(),
            TenantId = TestTenantContext.DefaultTenantId,
            BookingId = Guid.NewGuid(),
            ClientId = Guid.NewGuid(),
            InvoiceNumber = "2026-0003",
            InvoiceDate = new DateOnly(2026, 3, 25),
            SubTotalAmount = 30.00m,
            TotalVatAmount = 6.30m,
            TotalAmount = 36.30m,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
        db.Bookings.Add(new Booking
        {
            Id = unpaidInvoice.BookingId,
            TenantId = TestTenantContext.DefaultTenantId,
            ClientId = unpaidInvoice.ClientId,
            StaffMemberId = Guid.NewGuid(),
            StartTime = DateTimeOffset.UtcNow.AddHours(-2),
            EndTime = DateTimeOffset.UtcNow.AddHours(-1),
            CreatedAtUtc = DateTimeOffset.UtcNow.AddDays(-1),
        });
        db.Invoices.Add(unpaidInvoice);
        await db.SaveChangesAsync();

        var handler = new GetRevenueReportHandler(db, tenantContext);
        var result = await handler.Handle(new GetRevenueReportQuery("week", new DateOnly(2026, 3, 23)));

        var report = result.AsT0;
        Assert.Single(report.Rows);
        Assert.Equal("2026-0001", report.Rows[0].InvoiceNumber);
    }

    [Fact]
    public async Task GetRevenueReportHandler_EmptyPeriod_ReturnsEmptyRowsAndTotals()
    {
        await using var db = CreateDbContext();
        var tenantContext = TestTenantContext.Create();
        CreateTenantSettings(db, "Salon Test");

        var handler = new GetRevenueReportHandler(db, tenantContext);
        var result = await handler.Handle(new GetRevenueReportQuery("week", new DateOnly(2026, 3, 23)));

        var report = result.AsT0;
        Assert.Empty(report.Rows);
        Assert.Empty(report.DailyTotals);
        Assert.Equal(0, report.GrandTotal.InvoiceCount);
        Assert.Equal(0m, report.GrandTotal.TotalAmount);
    }

    [Fact]
    public async Task GetRevenueReportHandler_ReturnsCorrectDailyTotals()
    {
        await using var db = CreateDbContext();
        var tenantContext = TestTenantContext.Create();
        CreateTenantSettings(db, "Salon Test");

        CreatePaidInvoice(db, new DateOnly(2026, 3, 23), "2026-0001", 65.00m, 11.30m, PaymentMethod.Pin);
        CreatePaidInvoice(db, new DateOnly(2026, 3, 23), "2026-0002", 45.00m, 7.82m, PaymentMethod.Cash);
        CreatePaidInvoice(db, new DateOnly(2026, 3, 24), "2026-0003", 120.00m, 20.83m, PaymentMethod.BankTransfer);

        var handler = new GetRevenueReportHandler(db, tenantContext);
        var result = await handler.Handle(new GetRevenueReportQuery("week", new DateOnly(2026, 3, 23)));

        var report = result.AsT0;
        Assert.Equal(3, report.Rows.Count);
        Assert.Equal(2, report.DailyTotals.Count);

        var day1 = report.DailyTotals[0];
        Assert.Equal(new DateOnly(2026, 3, 23), day1.Date);
        Assert.Equal(110.00m, day1.TotalAmount);
        Assert.Equal(2, day1.InvoiceCount);

        var day2 = report.DailyTotals[1];
        Assert.Equal(new DateOnly(2026, 3, 24), day2.Date);
        Assert.Equal(120.00m, day2.TotalAmount);
        Assert.Equal(1, day2.InvoiceCount);
    }

    [Fact]
    public async Task GetRevenueReportHandler_ReturnsCorrectGrandTotal()
    {
        await using var db = CreateDbContext();
        var tenantContext = TestTenantContext.Create();
        CreateTenantSettings(db, "Salon Test");

        CreatePaidInvoice(db, new DateOnly(2026, 3, 23), "2026-0001", 65.00m, 11.30m);
        CreatePaidInvoice(db, new DateOnly(2026, 3, 24), "2026-0002", 120.00m, 20.83m);

        var handler = new GetRevenueReportHandler(db, tenantContext);
        var result = await handler.Handle(new GetRevenueReportQuery("week", new DateOnly(2026, 3, 23)));

        var report = result.AsT0;
        Assert.Equal(185.00m, report.GrandTotal.TotalAmount);
        Assert.Equal(32.13m, report.GrandTotal.VatAmount);
        Assert.Equal(2, report.GrandTotal.InvoiceCount);
    }

    [Fact]
    public async Task GetRevenueReportHandler_ReturnsSalonNameFromTenantSettings()
    {
        await using var db = CreateDbContext();
        var tenantContext = TestTenantContext.Create();
        CreateTenantSettings(db, "Kapsalon Mooi");

        var handler = new GetRevenueReportHandler(db, tenantContext);
        var result = await handler.Handle(new GetRevenueReportQuery("week", new DateOnly(2026, 3, 23)));

        var report = result.AsT0;
        Assert.Equal("Kapsalon Mooi", report.SalonName);
    }

    [Fact]
    public async Task GetRevenueReportHandler_NoTenantSettings_ReturnsOnbekend()
    {
        await using var db = CreateDbContext();
        var tenantContext = TestTenantContext.Create();

        var handler = new GetRevenueReportHandler(db, tenantContext);
        var result = await handler.Handle(new GetRevenueReportQuery("week", new DateOnly(2026, 3, 23)));

        var report = result.AsT0;
        Assert.Equal("Onbekend", report.SalonName);
    }

    [Fact]
    public async Task GetRevenueReportHandler_IncludesPaymentMethod()
    {
        await using var db = CreateDbContext();
        var tenantContext = TestTenantContext.Create();
        CreateTenantSettings(db, "Salon Test");

        CreatePaidInvoice(db, new DateOnly(2026, 3, 23), "2026-0001", 65.00m, 11.30m, PaymentMethod.Cash);

        var handler = new GetRevenueReportHandler(db, tenantContext);
        var result = await handler.Handle(new GetRevenueReportQuery("week", new DateOnly(2026, 3, 23)));

        var report = result.AsT0;
        Assert.Equal("Cash", report.Rows[0].PaymentMethod);
    }

    // ── GetRevenueReportPdf ──────────────────────────────────────────

    [Fact]
    public async Task GetRevenueReportPdfHandler_ValidRequest_ReturnsNonEmptyByteArray()
    {
        await using var db = CreateDbContext();
        var tenantContext = TestTenantContext.Create();
        CreateTenantSettings(db, "Salon Test");
        CreatePaidInvoice(db, new DateOnly(2026, 3, 23), "2026-0001", 65.00m, 11.30m);

        var pdfGenerator = new RevenueReportPdfGenerator();
        var handler = new GetRevenueReportPdfHandler(db, tenantContext, pdfGenerator);
        var result = await handler.Handle(new GetRevenueReportPdfQuery("week", new DateOnly(2026, 3, 23)));

        var pdf = result.AsT0;
        Assert.NotEmpty(pdf);
        Assert.True(pdf.Length > 0);
    }

    [Fact]
    public async Task GetRevenueReportPdfHandler_InvalidPeriod_ReturnsUnprocessable()
    {
        await using var db = CreateDbContext();
        var tenantContext = TestTenantContext.Create();

        var pdfGenerator = new RevenueReportPdfGenerator();
        var handler = new GetRevenueReportPdfHandler(db, tenantContext, pdfGenerator);
        var result = await handler.Handle(new GetRevenueReportPdfQuery("invalid", new DateOnly(2026, 3, 23)));

        Assert.True(result.IsT1);
    }

    // ── PDF Generator ────────────────────────────────────────────────

    [Fact]
    public void RevenueReportPdfGenerator_ProducesNonEmptyByteArray()
    {
        var generator = new RevenueReportPdfGenerator();
        var data = new RevenueReportResponse(
            "week",
            new DateOnly(2026, 3, 23),
            new DateOnly(2026, 3, 29),
            "Kapsalon Test",
            [
                new RevenueReportRow(new DateOnly(2026, 3, 23), "2026-0042", 65.00m, 11.30m, "Pin"),
                new RevenueReportRow(new DateOnly(2026, 3, 24), "2026-0043", 45.00m, 7.82m, "Cash"),
            ],
            [
                new RevenueReportDailyTotal(new DateOnly(2026, 3, 23), 65.00m, 11.30m, 1),
                new RevenueReportDailyTotal(new DateOnly(2026, 3, 24), 45.00m, 7.82m, 1),
            ],
            new RevenueReportGrandTotal(110.00m, 19.12m, 2));

        var pdf = generator.Generate(data);

        Assert.NotEmpty(pdf);
        Assert.True(pdf.Length > 100);
    }

    [Fact]
    public void RevenueReportPdfGenerator_EmptyReport_ProducesNonEmptyByteArray()
    {
        var generator = new RevenueReportPdfGenerator();
        var data = new RevenueReportResponse(
            "month",
            new DateOnly(2026, 3, 1),
            new DateOnly(2026, 3, 31),
            "Salon Leeg",
            [],
            [],
            new RevenueReportGrandTotal(0m, 0m, 0));

        var pdf = generator.Generate(data);

        Assert.NotEmpty(pdf);
    }
}
