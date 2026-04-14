using Chairly.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Chairly.Api.Features.Newsletters.UnsubscribeNewsletter;

internal static class UnsubscribeNewsletterEndpoint
{
    private const string ConfirmationHtml = """
        <!DOCTYPE html>
        <html lang="nl"><head><meta charset="utf-8"><title>Uitgeschreven</title></head>
        <body style="font-family: Arial, sans-serif; max-width: 480px; margin: 60px auto; text-align: center;">
        <h1>U bent uitgeschreven van onze nieuwsbrief.</h1>
        <p>U ontvangt geen marketing-e-mails meer van ons.</p>
        </body></html>
        """;

    private const string InvalidHtml = """
        <!DOCTYPE html>
        <html lang="nl"><head><meta charset="utf-8"><title>Ongeldige link</title></head>
        <body style="font-family: Arial, sans-serif; max-width: 480px; margin: 60px auto; text-align: center;">
        <h1>Ongeldige uitschrijflink</h1>
        <p>De gebruikte link is niet (meer) geldig.</p>
        </body></html>
        """;

    public static IEndpointRouteBuilder MapUnsubscribeNewsletter(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/newsletters/unsubscribe/{token}", [AllowAnonymous] async (
            string token,
            ChairlyDbContext db,
            CancellationToken cancellationToken) =>
        {
            var delivery = await db.NewsletterDeliveries
                .FirstOrDefaultAsync(d => d.UnsubscribeToken == token, cancellationToken)
                .ConfigureAwait(false);

            if (delivery is null)
            {
                return Results.Content(InvalidHtml, "text/html", statusCode: StatusCodes.Status404NotFound);
            }

            delivery.UnsubscribedAtUtc ??= DateTimeOffset.UtcNow;

            var client = await db.Clients
                .FirstOrDefaultAsync(c => c.Id == delivery.ClientId && c.TenantId == delivery.TenantId, cancellationToken)
                .ConfigureAwait(false);

            if (client is { })
            {
                client.IsSubscribedToNewsletter = false;
            }

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return Results.Content(ConfirmationHtml, "text/html");
        }).AllowAnonymous();

        return app;
    }
}
