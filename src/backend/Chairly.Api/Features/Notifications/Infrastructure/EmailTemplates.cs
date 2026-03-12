using System.Globalization;

namespace Chairly.Api.Features.Notifications.Infrastructure;

internal static class EmailTemplates
{
    private const string SalonName = "Uw salon";

    public static (string Subject, string HtmlBody) BookingConfirmation(string clientName, DateTimeOffset startTime, string serviceSummary)
    {
        var subject = $"Bevestiging van uw afspraak bij {SalonName}";
        var formattedDate = startTime.ToString("dddd d MMMM yyyy 'om' HH:mm", new CultureInfo("nl-NL"));
        var htmlBody = $"""
            <html>
            <body style="font-family: Arial, sans-serif; color: #333;">
                <h2>Beste {clientName},</h2>
                <p>Uw afspraak is bevestigd.</p>
                <p><strong>Datum en tijd:</strong> {formattedDate}</p>
                <p><strong>Diensten:</strong> {serviceSummary}</p>
                <p>Wij kijken ernaar uit u te verwelkomen!</p>
                <hr />
                <p style="font-size: 12px; color: #999;">Met vriendelijke groet,<br />{SalonName}</p>
            </body>
            </html>
            """;

        return (subject, htmlBody);
    }

    public static (string Subject, string HtmlBody) BookingReminder(string clientName, DateTimeOffset startTime, string serviceSummary)
    {
        var subject = $"Herinnering: uw afspraak morgen bij {SalonName}";
        var formattedDate = startTime.ToString("dddd d MMMM yyyy 'om' HH:mm", new CultureInfo("nl-NL"));
        var htmlBody = $"""
            <html>
            <body style="font-family: Arial, sans-serif; color: #333;">
                <h2>Beste {clientName},</h2>
                <p>Dit is een herinnering dat u morgen een afspraak heeft.</p>
                <p><strong>Datum en tijd:</strong> {formattedDate}</p>
                <p><strong>Diensten:</strong> {serviceSummary}</p>
                <p>Wij zien u graag!</p>
                <hr />
                <p style="font-size: 12px; color: #999;">Met vriendelijke groet,<br />{SalonName}</p>
            </body>
            </html>
            """;

        return (subject, htmlBody);
    }

    public static (string Subject, string HtmlBody) BookingCancellation(string clientName, DateTimeOffset startTime)
    {
        var subject = "Uw afspraak is geannuleerd";
        var formattedDate = startTime.ToString("dddd d MMMM yyyy 'om' HH:mm", new CultureInfo("nl-NL"));
        var htmlBody = $"""
            <html>
            <body style="font-family: Arial, sans-serif; color: #333;">
                <h2>Beste {clientName},</h2>
                <p>Uw afspraak is helaas geannuleerd.</p>
                <p><strong>Oorspronkelijke datum en tijd:</strong> {formattedDate}</p>
                <p>Neem gerust contact met ons op als u een nieuwe afspraak wilt maken.</p>
                <hr />
                <p style="font-size: 12px; color: #999;">Met vriendelijke groet,<br />{SalonName}</p>
            </body>
            </html>
            """;

        return (subject, htmlBody);
    }
}
