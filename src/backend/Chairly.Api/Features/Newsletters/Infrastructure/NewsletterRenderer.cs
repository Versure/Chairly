using System.Globalization;
using System.Web;

namespace Chairly.Api.Features.Newsletters.Infrastructure;

internal static class NewsletterRenderer
{
    public static string Render(string sanitisedBodyHtml, string salonName, string unsubscribeUrl)
    {
        var encodedSalon = HttpUtility.HtmlEncode(salonName ?? string.Empty);
        var encodedUnsub = HttpUtility.HtmlEncode(unsubscribeUrl ?? string.Empty);
        return string.Create(CultureInfo.InvariantCulture, $"""
            <!DOCTYPE html>
            <html lang="nl">
            <head><meta charset="utf-8"><title>{encodedSalon}</title></head>
            <body style="font-family: Arial, sans-serif; color: #222; max-width: 640px; margin: 0 auto; padding: 24px;">
            <div>{sanitisedBodyHtml}</div>
            <hr style="margin-top: 32px; border: none; border-top: 1px solid #ddd;" />
            <p style="font-size: 12px; color: #777; text-align: center;">
            U ontvangt deze e-mail omdat u bent ingeschreven voor de nieuwsbrief van {encodedSalon}.<br />
            <a href="{encodedUnsub}">Uitschrijven</a>
            </p>
            </body>
            </html>
            """);
    }
}
