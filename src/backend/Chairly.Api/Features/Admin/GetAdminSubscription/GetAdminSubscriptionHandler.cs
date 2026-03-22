using Chairly.Api.Shared.Mediator;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;

#pragma warning disable CA1812
namespace Chairly.Api.Features.Admin.GetAdminSubscription;

internal sealed class GetAdminSubscriptionHandler(WebsiteDbContext db) : IRequestHandler<GetAdminSubscriptionQuery, OneOf<AdminSubscriptionDetailResponse, NotFound>>
{
    public async Task<OneOf<AdminSubscriptionDetailResponse, NotFound>> Handle(GetAdminSubscriptionQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var entity = await db.Subscriptions
            .FirstOrDefaultAsync(s => s.Id == query.Id, cancellationToken)
            .ConfigureAwait(false);

        if (entity is null)
        {
            return new NotFound();
        }

        return AdminSubscriptionMapper.ToDetailResponse(entity);
    }
}
#pragma warning restore CA1812
