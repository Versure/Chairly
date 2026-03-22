using System.Security.Claims;
using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Results;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;

#pragma warning disable CA1812
namespace Chairly.Api.Features.Admin.ProvisionSubscription;

internal sealed class ProvisionSubscriptionHandler(WebsiteDbContext db, IHttpContextAccessor httpContextAccessor) : IRequestHandler<ProvisionSubscriptionCommand, OneOf<AdminSubscriptionDetailResponse, NotFound, Unprocessable>>
{
    public async Task<OneOf<AdminSubscriptionDetailResponse, NotFound, Unprocessable>> Handle(ProvisionSubscriptionCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var entity = await db.Subscriptions
            .FirstOrDefaultAsync(s => s.Id == command.Id, cancellationToken)
            .ConfigureAwait(false);

        if (entity is null)
        {
            return new NotFound();
        }

        if (entity.ProvisionedAtUtc is not null)
        {
            return new Unprocessable("Abonnement is al geactiveerd.");
        }

        if (entity.CancelledAtUtc is not null)
        {
            return new Unprocessable("Geannuleerd abonnement kan niet worden geactiveerd.");
        }

        entity.ProvisionedAtUtc = DateTimeOffset.UtcNow;
        entity.ProvisionedBy = GetAdminUserId();

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return AdminSubscriptionMapper.ToDetailResponse(entity);
    }

    private Guid? GetAdminUserId()
    {
        var sub = httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value;
        return sub is not null && Guid.TryParse(sub, out var userId) ? userId : null;
    }
}
#pragma warning restore CA1812
