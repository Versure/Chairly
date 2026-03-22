using System.Security.Claims;
using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Results;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;

#pragma warning disable CA1812
namespace Chairly.Api.Features.Admin.CancelSubscription;

internal sealed class CancelSubscriptionHandler(WebsiteDbContext db, IHttpContextAccessor httpContextAccessor) : IRequestHandler<CancelSubscriptionCommand, OneOf<AdminSubscriptionDetailResponse, NotFound, Unprocessable>>
{
    public async Task<OneOf<AdminSubscriptionDetailResponse, NotFound, Unprocessable>> Handle(CancelSubscriptionCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var entity = await db.Subscriptions
            .FirstOrDefaultAsync(s => s.Id == command.Id, cancellationToken)
            .ConfigureAwait(false);

        if (entity is null)
        {
            return new NotFound();
        }

        if (entity.CancelledAtUtc is not null)
        {
            return new Unprocessable("Abonnement is al geannuleerd.");
        }

        entity.CancelledAtUtc = DateTimeOffset.UtcNow;
        entity.CancelledBy = GetAdminUserId();
        entity.CancellationReason = command.CancellationReason;

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
