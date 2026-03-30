using System.Globalization;
using Chairly.Api.Features.Notifications.Infrastructure;
using Chairly.Api.Features.Notifications.UpdateEmailTemplate;
using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Tenancy;
using Chairly.Domain.Enums;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using OneOf;

#pragma warning disable CA1812 // Instantiated via DI
namespace Chairly.Api.Features.Notifications.PreviewEmailTemplate;

internal sealed class PreviewEmailTemplateHandler(ChairlyDbContext db, ITenantContext tenantContext)
    : IRequestHandler<PreviewEmailTemplateCommand, OneOf<PreviewEmailTemplateResponse, BadRequest>>
{
    public async Task<OneOf<PreviewEmailTemplateResponse, BadRequest>> Handle(PreviewEmailTemplateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!Enum.TryParse<NotificationType>(command.TemplateType, ignoreCase: false, out var notificationType))
        {
            return new BadRequest();
        }

        var settings = await db.TenantSettings
            .FirstOrDefaultAsync(s => s.TenantId == tenantContext.TenantId, cancellationToken)
            .ConfigureAwait(false);
        var salonName = settings?.CompanyName ?? "Uw salon";

        var subject = ReplacePlaceholders(command.Subject, notificationType, salonName);
        var body = ReplacePlaceholders(command.Body, notificationType, salonName);

        var htmlBody = EmailTemplates.BuildTemplateFromBody(salonName, body);

        return new PreviewEmailTemplateResponse(subject, htmlBody);
    }

    private static string ReplacePlaceholders(string text, NotificationType type, string salonName)
    {
        var result = text
            .Replace("{clientName}", "Jan de Vries", StringComparison.Ordinal)
            .Replace("{salonName}", salonName, StringComparison.Ordinal)
            .Replace("{date}", DateTimeOffset.Now.ToString("dddd d MMMM yyyy 'om' HH:mm", new CultureInfo("nl-NL")), StringComparison.Ordinal);

        if (type is NotificationType.BookingConfirmation or NotificationType.BookingReminder
            or NotificationType.BookingReceived)
        {
            result = result.Replace("{services}", "Heren knippen, Baard trimmen", StringComparison.Ordinal);
        }

        if (type == NotificationType.InvoiceSent)
        {
            result = result
                .Replace("{invoiceNumber}", "F-2026-001", StringComparison.Ordinal)
                .Replace("{invoiceDate}", DateOnly.FromDateTime(DateTime.Today).ToString("d MMMM yyyy", new CultureInfo("nl-NL")), StringComparison.Ordinal)
                .Replace("{totalAmount}", 75.00m.ToString("C", new CultureInfo("nl-NL")), StringComparison.Ordinal);
        }

        return result;
    }
}
#pragma warning restore CA1812
