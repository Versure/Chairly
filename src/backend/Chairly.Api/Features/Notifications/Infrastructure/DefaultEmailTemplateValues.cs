using Chairly.Domain.Enums;

namespace Chairly.Api.Features.Notifications.Infrastructure;

internal static class DefaultEmailTemplateValues
{
    internal sealed record TemplateDefaults(
        string Subject,
        string MainMessage,
        string ClosingMessage,
        string[] AvailablePlaceholders);

    internal static TemplateDefaults GetDefaults(NotificationType type, string salonName)
    {
        return type switch
        {
            NotificationType.BookingConfirmation => new(
                $"Bevestiging van uw afspraak bij {salonName}",
                "Uw afspraak is bevestigd.",
                "Wij kijken ernaar uit u te verwelkomen!",
                ["clientName", "salonName", "date", "services"]),
            NotificationType.BookingReminder => new(
                $"Herinnering: uw afspraak morgen bij {salonName}",
                "Dit is een herinnering dat u morgen een afspraak heeft.",
                "Wij zien u graag!",
                ["clientName", "salonName", "date", "services"]),
            NotificationType.BookingCancellation => new(
                "Uw afspraak is geannuleerd",
                "Uw afspraak is helaas geannuleerd.",
                "Neem gerust contact met ons op als u een nieuwe afspraak wilt maken.",
                ["clientName", "salonName", "date"]),
            NotificationType.BookingReceived => new(
                $"Nieuwe boeking bij {salonName}",
                "Wij hebben uw boeking ontvangen. Uw boeking wacht op bevestiging.",
                "Wij nemen zo snel mogelijk contact met u op.",
                ["clientName", "salonName", "date", "services"]),
            NotificationType.InvoiceSent => new(
                $"Factuur {{invoiceNumber}} van {salonName}",
                "Bedankt voor uw bezoek! Bijgaand vindt u uw factuur.",
                "Wij zien u graag terug!",
                ["clientName", "salonName", "invoiceNumber", "invoiceDate", "totalAmount"]),
            _ => new(string.Empty, string.Empty, string.Empty, []),
        };
    }
}
