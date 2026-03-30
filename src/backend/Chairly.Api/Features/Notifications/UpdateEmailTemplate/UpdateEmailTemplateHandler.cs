using Chairly.Api.Features.Notifications.Infrastructure;
using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Tenancy;
using Chairly.Domain.Entities;
using Chairly.Domain.Enums;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using OneOf;

#pragma warning disable CA1812 // Instantiated via DI
namespace Chairly.Api.Features.Notifications.UpdateEmailTemplate;

internal sealed class UpdateEmailTemplateHandler(ChairlyDbContext db, ITenantContext tenantContext)
    : IRequestHandler<UpdateEmailTemplateCommand, OneOf<EmailTemplateResponse, BadRequest>>
{
    public async Task<OneOf<EmailTemplateResponse, BadRequest>> Handle(UpdateEmailTemplateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!Enum.TryParse<NotificationType>(command.TemplateType, ignoreCase: false, out var notificationType))
        {
            return new BadRequest();
        }

        var tenantId = tenantContext.TenantId;

        var existing = await db.EmailTemplates
            .FirstOrDefaultAsync(t => t.TenantId == tenantId && t.TemplateType == notificationType, cancellationToken)
            .ConfigureAwait(false);

        if (existing is not null)
        {
            existing.Subject = command.Subject;
            existing.MainMessage = command.MainMessage;
            existing.ClosingMessage = command.ClosingMessage;
            existing.DateLabel = command.DateLabel;
            existing.ServicesLabel = command.ServicesLabel;
            existing.UpdatedAtUtc = DateTimeOffset.UtcNow;
            existing.UpdatedBy = tenantContext.UserId;
        }
        else
        {
            existing = new EmailTemplate
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                TemplateType = notificationType,
                Subject = command.Subject,
                MainMessage = command.MainMessage,
                ClosingMessage = command.ClosingMessage,
                DateLabel = command.DateLabel,
                ServicesLabel = command.ServicesLabel,
                CreatedAtUtc = DateTimeOffset.UtcNow,
                CreatedBy = tenantContext.UserId,
            };
            db.EmailTemplates.Add(existing);
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var defaults = DefaultEmailTemplateValues.GetDefaults(notificationType, string.Empty);

        return new EmailTemplateResponse(
            notificationType.ToString(),
            existing.Subject,
            existing.MainMessage,
            existing.ClosingMessage,
            existing.DateLabel ?? defaults.DateLabel,
            existing.ServicesLabel ?? defaults.ServicesLabel,
            IsCustomized: true,
            defaults.AvailablePlaceholders);
    }
}
#pragma warning restore CA1812
