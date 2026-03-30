using Chairly.Api.Features.Notifications.Infrastructure;
using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Tenancy;
using Chairly.Domain.Enums;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

#pragma warning disable CA1812 // Instantiated via DI
namespace Chairly.Api.Features.Notifications.GetEmailTemplatesList;

internal sealed class GetEmailTemplatesListHandler(ChairlyDbContext db, ITenantContext tenantContext)
    : IRequestHandler<GetEmailTemplatesListQuery, List<EmailTemplateResponse>>
{
    public async Task<List<EmailTemplateResponse>> Handle(GetEmailTemplatesListQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var tenantId = tenantContext.TenantId;

        var customTemplates = await db.EmailTemplates
            .Where(t => t.TenantId == tenantId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var settings = await db.TenantSettings
            .FirstOrDefaultAsync(s => s.TenantId == tenantId, cancellationToken)
            .ConfigureAwait(false);
        var salonName = settings?.CompanyName ?? "Uw salon";

        var templateTypes = new[]
        {
            NotificationType.BookingConfirmation,
            NotificationType.BookingReminder,
            NotificationType.BookingCancellation,
            NotificationType.BookingReceived,
            NotificationType.InvoiceSent,
        };

        var results = new List<EmailTemplateResponse>(templateTypes.Length);

        foreach (var type in templateTypes)
        {
            var defaults = DefaultEmailTemplateValues.GetDefaults(type, salonName);
            var custom = customTemplates.Find(t => t.TemplateType == type);

            if (custom is not null)
            {
                results.Add(new EmailTemplateResponse(
                    type.ToString(),
                    custom.Subject,
                    custom.MainMessage,
                    custom.ClosingMessage,
                    custom.DateLabel ?? defaults.DateLabel,
                    custom.ServicesLabel ?? defaults.ServicesLabel,
                    IsCustomized: true,
                    defaults.AvailablePlaceholders));
            }
            else
            {
                results.Add(new EmailTemplateResponse(
                    type.ToString(),
                    defaults.Subject,
                    defaults.MainMessage,
                    defaults.ClosingMessage,
                    defaults.DateLabel,
                    defaults.ServicesLabel,
                    IsCustomized: false,
                    defaults.AvailablePlaceholders));
            }
        }

        return results;
    }
}
#pragma warning restore CA1812
