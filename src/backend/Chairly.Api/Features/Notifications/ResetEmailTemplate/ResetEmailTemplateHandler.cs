using Chairly.Api.Features.Notifications.UpdateEmailTemplate;
using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Tenancy;
using Chairly.Domain.Enums;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;

#pragma warning disable CA1812 // Instantiated via DI
namespace Chairly.Api.Features.Notifications.ResetEmailTemplate;

internal sealed class ResetEmailTemplateHandler(ChairlyDbContext db, ITenantContext tenantContext)
    : IRequestHandler<ResetEmailTemplateCommand, OneOf<Success, BadRequest>>
{
    public async Task<OneOf<Success, BadRequest>> Handle(ResetEmailTemplateCommand command, CancellationToken cancellationToken = default)
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
            db.EmailTemplates.Remove(existing);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return new Success();
    }
}
#pragma warning restore CA1812
