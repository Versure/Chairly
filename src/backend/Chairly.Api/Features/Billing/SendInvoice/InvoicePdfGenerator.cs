using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

#pragma warning disable CA1812 // Instantiated via DI
namespace Chairly.Api.Features.Billing.SendInvoice;

internal sealed class InvoicePdfGenerator : IInvoicePdfGenerator
{
    private static readonly CultureInfo _dutchCulture = new("nl-NL");

    public byte[] Generate(InvoicePdfData data)
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
                page.Footer().Element(ComposeFooter);
            });
        });

        return document.GeneratePdf();
    }

    private static void ComposeHeader(IContainer container, InvoicePdfData data)
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
                    right.Item().Text("FACTUUR").Bold().FontSize(22).FontColor(Colors.Grey.Darken2);
                });
            });

            column.Item().PaddingTop(20).Row(row =>
            {
                row.RelativeItem().Column(left =>
                {
                    left.Item().Text("Klant").SemiBold().FontSize(9).FontColor(Colors.Grey.Medium);
                    left.Item().Text(data.ClientName);
                });

                row.RelativeItem().AlignRight().Column(right =>
                {
                    right.Item().Text($"Factuurnummer: {data.InvoiceNumber}");
                    right.Item().Text($"Factuurdatum: {data.InvoiceDate.ToString("d MMMM yyyy", _dutchCulture)}");
                });
            });

            if (data.IsPaid)
            {
                column.Item().PaddingTop(12).Row(row =>
                {
                    row.RelativeItem().AlignRight().Element(c =>
                    {
                        c.Background(Colors.Green.Lighten4)
                            .Padding(6)
                            .Text("BETAALD")
                            .Bold()
                            .FontSize(12)
                            .FontColor(Colors.Green.Darken3);
                    });
                });
            }

            column.Item().PaddingTop(16).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
        });
    }

    private static void ComposeContent(IContainer container, InvoicePdfData data)
    {
        container.PaddingTop(16).Column(column =>
        {
            column.Item().Element(c => ComposeLineItemsTable(c, data));
            column.Item().Element(c => ComposeTotals(c, data));
        });
    }

    private static void ComposeLineItemsTable(IContainer container, InvoicePdfData data)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(4); // Omschrijving
                columns.RelativeColumn(1); // Aantal
                columns.RelativeColumn(1.5f); // Prijs
                columns.RelativeColumn(1); // BTW
                columns.RelativeColumn(1.5f); // Totaal
            });

            table.Header(header =>
            {
                header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(4)
                    .Text("Omschrijving").SemiBold().FontSize(9);
                header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(4).AlignRight()
                    .Text("Aantal").SemiBold().FontSize(9);
                header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(4).AlignRight()
                    .Text("Prijs").SemiBold().FontSize(9);
                header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(4).AlignRight()
                    .Text("BTW").SemiBold().FontSize(9);
                header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(4).AlignRight()
                    .Text("Totaal").SemiBold().FontSize(9);
            });

            foreach (var item in data.LineItems)
            {
                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(4)
                    .Text(item.Description);
                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(4).AlignRight()
                    .Text(item.Quantity.ToString(_dutchCulture));
                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(4).AlignRight()
                    .Text(item.UnitPrice.ToString("C", _dutchCulture));
                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(4).AlignRight()
                    .Text($"{item.VatPercentage:0.##}%");
                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(4).AlignRight()
                    .Text(item.LineTotal.ToString("C", _dutchCulture));
            }
        });
    }

    private static void ComposeTotals(IContainer container, InvoicePdfData data)
    {
        container.PaddingTop(12).AlignRight().Width(200).Column(totals =>
        {
            totals.Item().Row(row =>
            {
                row.RelativeItem().Text("Subtotaal");
                row.RelativeItem().AlignRight().Text(data.SubTotalAmount.ToString("C", _dutchCulture));
            });
            totals.Item().Row(row =>
            {
                row.RelativeItem().Text("BTW");
                row.RelativeItem().AlignRight().Text(data.TotalVatAmount.ToString("C", _dutchCulture));
            });
            totals.Item().PaddingTop(4).BorderTop(1).BorderColor(Colors.Grey.Lighten1).Row(row =>
            {
                row.RelativeItem().Text("Totaal").Bold();
                row.RelativeItem().AlignRight().Text(data.TotalAmount.ToString("C", _dutchCulture)).Bold();
            });
        });
    }

    private static void ComposeFooter(IContainer container)
    {
        container.AlignCenter().Text(text =>
        {
            text.DefaultTextStyle(x => x.FontSize(8).FontColor(Colors.Grey.Medium));
            text.Span("Pagina ");
            text.CurrentPageNumber();
            text.Span(" van ");
            text.TotalPages();
        });
    }
}
#pragma warning restore CA1812
