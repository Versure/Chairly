using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

#pragma warning disable CA1812 // Instantiated via DI
namespace Chairly.Api.Features.Reports;

internal sealed class RevenueReportPdfGenerator : IRevenueReportPdfGenerator
{
    private static readonly CultureInfo _dutchCulture = new("nl-NL");

    public byte[] Generate(RevenueReportResponse data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginTop(40);
                page.MarginBottom(40);
                page.MarginHorizontal(50);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(c => ComposeHeader(c, data));
                page.Content().Element(c => ComposeContent(c, data));
                page.Footer().Element(c => ComposeFooter(c));
            });
        });

        return document.GeneratePdf();
    }

    private static void ComposeHeader(IContainer container, RevenueReportResponse data)
    {
        container.Column(column =>
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().Column(left =>
                {
                    left.Item().Text(data.SalonName).Bold().FontSize(18).FontColor(Colors.Indigo.Darken2);
                });

                row.RelativeItem().AlignRight().Column(right =>
                {
                    right.Item().Text("OMZETRAPPORT").Bold().FontSize(22).FontColor(Colors.Grey.Darken2);
                });
            });

            column.Item().PaddingTop(12).Text(FormatPeriodLabel(data)).FontSize(12).SemiBold();

            column.Item().PaddingTop(16).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
        });
    }

    private static void ComposeContent(IContainer container, RevenueReportResponse data)
    {
        container.PaddingTop(16).Column(column =>
        {
            if (data.Rows.Count == 0)
            {
                column.Item().PaddingVertical(20).AlignCenter()
                    .Text("Geen betaalde facturen in deze periode.").FontSize(11).FontColor(Colors.Grey.Medium);
                return;
            }

            column.Item().Element(c => ComposeTable(c, data));
        });
    }

    private static void ComposeTable(IContainer container, RevenueReportResponse data)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(1.5f); // Datum
                columns.RelativeColumn(2);    // Factuurnummer
                columns.RelativeColumn(1.5f); // Bedrag (incl. BTW)
                columns.RelativeColumn(1.5f); // BTW
                columns.RelativeColumn(1.5f); // Betaalmethode
            });

            table.Header(header =>
            {
                header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(4)
                    .Text("Datum").SemiBold().FontSize(9);
                header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(4)
                    .Text("Factuurnummer").SemiBold().FontSize(9);
                header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(4).AlignRight()
                    .Text("Bedrag (incl. BTW)").SemiBold().FontSize(9);
                header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(4).AlignRight()
                    .Text("BTW").SemiBold().FontSize(9);
                header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(4)
                    .Text("Betaalmethode").SemiBold().FontSize(9);
            });

            var rowsByDate = data.Rows
                .GroupBy(r => r.Date)
                .OrderBy(g => g.Key);

            foreach (var dateGroup in rowsByDate)
            {
                foreach (var row in dateGroup)
                {
                    ComposeDataCell(table, row.Date.ToString("dd-MM-yyyy", _dutchCulture));
                    ComposeDataCell(table, row.InvoiceNumber);
                    ComposeDataCell(table, row.TotalAmount.ToString("C", _dutchCulture), alignRight: true);
                    ComposeDataCell(table, row.VatAmount.ToString("C", _dutchCulture), alignRight: true);
                    ComposeDataCell(table, FormatPaymentMethod(row.PaymentMethod));
                }

                var dailyTotal = data.DailyTotals.FirstOrDefault(d => d.Date == dateGroup.Key);
                if (dailyTotal != null)
                {
                    ComposeSubtotalCell(table, $"Subtotaal {dateGroup.Key.ToString("dd-MM-yyyy", _dutchCulture)}");
                    ComposeSubtotalCell(table, string.Empty);
                    ComposeSubtotalCell(table, dailyTotal.TotalAmount.ToString("C", _dutchCulture), alignRight: true);
                    ComposeSubtotalCell(table, dailyTotal.VatAmount.ToString("C", _dutchCulture), alignRight: true);
                    ComposeSubtotalCell(table, $"{dailyTotal.InvoiceCount} facturen");
                }
            }

            // Grand total row
            ComposeGrandTotalCell(table, "Totaal");
            ComposeGrandTotalCell(table, string.Empty);
            ComposeGrandTotalCell(table, data.GrandTotal.TotalAmount.ToString("C", _dutchCulture), alignRight: true);
            ComposeGrandTotalCell(table, data.GrandTotal.VatAmount.ToString("C", _dutchCulture), alignRight: true);
            ComposeGrandTotalCell(table, $"{data.GrandTotal.InvoiceCount} facturen");
        });
    }

    private static void ComposeDataCell(TableDescriptor table, string text, bool alignRight = false)
    {
        var cell = table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(4);
        if (alignRight)
        {
            cell.AlignRight().Text(text);
        }
        else
        {
            cell.Text(text);
        }
    }

    private static void ComposeSubtotalCell(TableDescriptor table, string text, bool alignRight = false)
    {
        var cell = table.Cell().Background(Colors.Grey.Lighten3).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4);
        if (alignRight)
        {
            cell.AlignRight().Text(text).Bold().FontSize(9);
        }
        else
        {
            cell.Text(text).Bold().FontSize(9);
        }
    }

    private static void ComposeGrandTotalCell(TableDescriptor table, string text, bool alignRight = false)
    {
        var cell = table.Cell().Background(Colors.Grey.Lighten2).BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(6);
        if (alignRight)
        {
            cell.AlignRight().Text(text).Bold().FontSize(11);
        }
        else
        {
            cell.Text(text).Bold().FontSize(11);
        }
    }

    private static void ComposeFooter(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().AlignCenter().Text(text =>
            {
                text.DefaultTextStyle(x => x.FontSize(8).FontColor(Colors.Grey.Medium));
                text.Span("Pagina ");
                text.CurrentPageNumber();
                text.Span(" van ");
                text.TotalPages();
            });

            column.Item().AlignCenter().Text(
                $"Gegenereerd op {DateTimeOffset.UtcNow.ToString("dd-MM-yyyy HH:mm", _dutchCulture)} UTC")
                .FontSize(7).FontColor(Colors.Grey.Medium);
        });
    }

    private static string FormatPeriodLabel(RevenueReportResponse data)
    {
        if (string.Equals(data.PeriodType, "week", StringComparison.OrdinalIgnoreCase))
        {
            var weekNumber = ISOWeek.GetWeekOfYear(data.PeriodStart.ToDateTime(TimeOnly.MinValue));
            return $"Week {weekNumber}: {data.PeriodStart.ToString("d MMMM", _dutchCulture)} - {data.PeriodEnd.ToString("d MMMM yyyy", _dutchCulture)}";
        }

        return data.PeriodStart.ToString("MMMM yyyy", _dutchCulture);
    }

    private static string FormatPaymentMethod(string paymentMethod)
    {
        return paymentMethod switch
        {
            "Cash" => "Contant",
            "Pin" => "Pin",
            "BankTransfer" => "Overboeking",
            _ => paymentMethod,
        };
    }
}
#pragma warning restore CA1812
