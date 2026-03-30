using System.Globalization;

namespace Chairly.Api.Features.Notifications.Infrastructure;

internal static class EmailTemplates
{
    private static string FormatDutchDate(DateTimeOffset startTime)
    {
        var dutchTime = TimeZoneInfo.ConvertTime(startTime, TimeZoneInfo.FindSystemTimeZoneById("Europe/Amsterdam"));
        return dutchTime.ToString("dddd d MMMM yyyy 'om' HH:mm", new CultureInfo("nl-NL"));
    }

    public static (string Subject, string HtmlBody) BookingConfirmation(string clientName, DateTimeOffset startTime, string serviceSummary, string salonName)
    {
        var subject = $"Bevestiging van uw afspraak bij {salonName}";
        var formattedDate = FormatDutchDate(startTime);
        var htmlBody = BuildTemplate(
            salonName,
            clientName,
            "Uw afspraak is bevestigd.",
            formattedDate,
            serviceSummary,
            "Wij kijken ernaar uit u te verwelkomen!");

        return (subject, htmlBody);
    }

    public static (string Subject, string HtmlBody) BookingReminder(string clientName, DateTimeOffset startTime, string serviceSummary, string salonName)
    {
        var subject = $"Herinnering: uw afspraak morgen bij {salonName}";
        var formattedDate = FormatDutchDate(startTime);
        var htmlBody = BuildTemplate(
            salonName,
            clientName,
            "Dit is een herinnering dat u morgen een afspraak heeft.",
            formattedDate,
            serviceSummary,
            "Wij zien u graag!");

        return (subject, htmlBody);
    }

    public static (string Subject, string HtmlBody) BookingReceived(string clientName, DateTimeOffset startTime, string serviceSummary, string salonName)
    {
        var subject = $"Nieuwe boeking bij {salonName}";
        var formattedDate = FormatDutchDate(startTime);
        var htmlBody = BuildTemplate(
            salonName,
            clientName,
            "Wij hebben uw boeking ontvangen. Uw boeking wacht op bevestiging.",
            formattedDate,
            serviceSummary,
            "Wij nemen zo snel mogelijk contact met u op.");

        return (subject, htmlBody);
    }

    public static (string Subject, string HtmlBody) BookingCancellation(string clientName, DateTimeOffset startTime, string salonName)
    {
        var subject = "Uw afspraak is geannuleerd";
        var formattedDate = FormatDutchDate(startTime);
        var htmlBody = BuildTemplate(
            salonName,
            clientName,
            "Uw afspraak is helaas geannuleerd.",
            formattedDate,
            null,
            "Neem gerust contact met ons op als u een nieuwe afspraak wilt maken.",
            "Oorspronkelijke datum en tijd");

        return (subject, htmlBody);
    }

    public static (string Subject, string HtmlBody) InvoiceSent(
        string clientName,
        string invoiceNumber,
        DateOnly invoiceDate,
        decimal totalAmount,
        string salonName,
        bool isPaid = false)
    {
        var subject = $"Factuur {invoiceNumber} van {salonName}";
        var formattedInvoiceDate = invoiceDate.ToString("d MMMM yyyy", new CultureInfo("nl-NL"));
        var formattedTotalAmount = totalAmount.ToString("C", new CultureInfo("nl-NL"));

        var paidBadge = isPaid
            ? """<p style="margin: 12px 0; padding: 8px 16px; background-color: #DEF7EC; color: #03543F; border-radius: 4px; font-weight: 600; display: inline-block;">&#10003; Deze factuur is reeds betaald.</p>"""
            : string.Empty;

        var htmlBody = BuildTemplate(
            salonName,
            clientName,
            "Bedankt voor uw bezoek! Bijgaand vindt u uw factuur.",
            formattedInvoiceDate,
            $"Factuurnummer: {invoiceNumber}<br />Totaalbedrag: {formattedTotalAmount}{(isPaid ? "<br />" + paidBadge : string.Empty)}",
            "Wij zien u graag terug!",
            "Factuurdatum");

        return (subject, htmlBody);
    }

    internal static string BuildTemplate(
        string salonName,
        string clientName,
        string mainMessage,
        string formattedDate,
        string? serviceSummary,
        string closingMessage,
        string dateLabel = "Datum en tijd",
        string servicesLabel = "Diensten")
    {
        var serviceSection = string.IsNullOrEmpty(serviceSummary)
            ? string.Empty
            : $"""
                              <p style="margin: 16px 0 8px; color: #6b7280; font-size: 14px;">{servicesLabel}</p>
                              <p style="margin: 0; color: #111827; font-weight: 600;">{serviceSummary}</p>
              """;

        return $"""
            <!DOCTYPE html>
            <html lang="nl">
            <head><meta charset="utf-8" /></head>
            <body style="margin: 0; padding: 0; background-color: #f3f4f6; font-family: Arial, sans-serif;">
              <table width="100%" cellpadding="0" cellspacing="0" style="background-color: #f3f4f6; padding: 32px 16px;">
                <tr><td align="center">
                  <table width="600" cellpadding="0" cellspacing="0" style="max-width: 600px; width: 100%;">
                    <tr><td style="background-color: #4F46E5; padding: 24px 32px; border-radius: 8px 8px 0 0;">
                      <h1 style="margin: 0; color: #ffffff; font-size: 20px;">{salonName}</h1>
                    </td></tr>
                    <tr><td style="background-color: #ffffff; padding: 32px; border-left: 1px solid #e5e7eb; border-right: 1px solid #e5e7eb;">
                      <h2 style="margin: 0 0 16px; color: #111827; font-size: 18px;">Beste {clientName},</h2>
                      <p style="margin: 0 0 16px; color: #374151; line-height: 1.6;">{mainMessage}</p>
                      <table width="100%" cellpadding="16" cellspacing="0" style="background-color: #f9fafb; border-radius: 6px; margin: 16px 0;">
                        <tr><td>
                          <p style="margin: 0 0 8px; color: #6b7280; font-size: 14px;">{dateLabel}</p>
                          <p style="margin: 0; color: #111827; font-weight: 600;">{formattedDate}</p>
            {serviceSection}
                        </td></tr>
                      </table>
                      <p style="margin: 16px 0 0; color: #374151; line-height: 1.6;">{closingMessage}</p>
                    </td></tr>
                    <tr><td style="background-color: #f9fafb; padding: 24px 32px; border-radius: 0 0 8px 8px; border: 1px solid #e5e7eb; border-top: none;">
                      <p style="margin: 0; color: #9ca3af; font-size: 13px;">Met vriendelijke groet,<br />{salonName}</p>
                    </td></tr>
                  </table>
                </td></tr>
              </table>
            </body>
            </html>
            """;
    }
}
