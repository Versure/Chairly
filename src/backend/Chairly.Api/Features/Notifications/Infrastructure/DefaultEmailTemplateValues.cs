using Chairly.Domain.Enums;

namespace Chairly.Api.Features.Notifications.Infrastructure;

internal static class DefaultEmailTemplateValues
{
    internal sealed record TemplateDefaults(
        string Subject,
        string Body,
        string[] AvailablePlaceholders);

    internal static TemplateDefaults GetDefaults(NotificationType type, string salonName)
    {
        return type switch
        {
            NotificationType.BookingConfirmation => new(
                $"Bevestiging van uw afspraak bij {salonName}",
                """
                <h2 style="margin: 0 0 16px; color: #111827; font-size: 18px;">Beste {clientName},</h2>
                <p>Uw afspraak is bevestigd.</p>
                <p><strong>Datum en tijd</strong><br>{date}</p>
                <p><strong>Diensten</strong><br>{services}</p>
                <p>Wij kijken ernaar uit u te verwelkomen!</p>
                <p style="margin-top: 24px; color: #9ca3af; font-size: 13px;">Met vriendelijke groet,<br>{salonName}</p>
                """,
                ["clientName", "salonName", "date", "services"]),
            NotificationType.BookingReminder => new(
                $"Herinnering: uw afspraak morgen bij {salonName}",
                """
                <h2 style="margin: 0 0 16px; color: #111827; font-size: 18px;">Beste {clientName},</h2>
                <p>Dit is een herinnering dat u morgen een afspraak heeft.</p>
                <p><strong>Datum en tijd</strong><br>{date}</p>
                <p><strong>Diensten</strong><br>{services}</p>
                <p>Wij zien u graag!</p>
                <p style="margin-top: 24px; color: #9ca3af; font-size: 13px;">Met vriendelijke groet,<br>{salonName}</p>
                """,
                ["clientName", "salonName", "date", "services"]),
            NotificationType.BookingCancellation => new(
                "Uw afspraak is geannuleerd",
                """
                <h2 style="margin: 0 0 16px; color: #111827; font-size: 18px;">Beste {clientName},</h2>
                <p>Uw afspraak is helaas geannuleerd.</p>
                <p><strong>Oorspronkelijke datum en tijd</strong><br>{date}</p>
                <p>Neem gerust contact met ons op als u een nieuwe afspraak wilt maken.</p>
                <p style="margin-top: 24px; color: #9ca3af; font-size: 13px;">Met vriendelijke groet,<br>{salonName}</p>
                """,
                ["clientName", "salonName", "date"]),
            NotificationType.BookingReceived => new(
                $"Nieuwe boeking bij {salonName}",
                """
                <h2 style="margin: 0 0 16px; color: #111827; font-size: 18px;">Beste {clientName},</h2>
                <p>Wij hebben uw boeking ontvangen. Uw boeking wacht op bevestiging.</p>
                <p><strong>Datum en tijd</strong><br>{date}</p>
                <p><strong>Diensten</strong><br>{services}</p>
                <p>Wij nemen zo snel mogelijk contact met u op.</p>
                <p style="margin-top: 24px; color: #9ca3af; font-size: 13px;">Met vriendelijke groet,<br>{salonName}</p>
                """,
                ["clientName", "salonName", "date", "services"]),
            NotificationType.InvoiceSent => new(
                $"Factuur {{invoiceNumber}} van {salonName}",
                """
                <h2 style="margin: 0 0 16px; color: #111827; font-size: 18px;">Beste {clientName},</h2>
                <p>Bedankt voor uw bezoek! Bijgaand vindt u uw factuur.</p>
                <p><strong>Factuurdatum</strong><br>{invoiceDate}</p>
                <p><strong>Factuurnummer</strong><br>{invoiceNumber}</p>
                <p><strong>Totaalbedrag</strong><br>{totalAmount}</p>
                <p>Wij zien u graag terug!</p>
                <p style="margin-top: 24px; color: #9ca3af; font-size: 13px;">Met vriendelijke groet,<br>{salonName}</p>
                """,
                ["clientName", "salonName", "invoiceNumber", "invoiceDate", "totalAmount"]),
            _ => new(string.Empty, string.Empty, []),
        };
    }
}
