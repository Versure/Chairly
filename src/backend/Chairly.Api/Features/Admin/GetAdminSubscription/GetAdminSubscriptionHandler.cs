using Chairly.Api.Shared.Mediator;
using Chairly.Infrastructure.Keycloak;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OneOf;
using OneOf.Types;

#pragma warning disable CA1812
namespace Chairly.Api.Features.Admin.GetAdminSubscription;

internal sealed class GetAdminSubscriptionHandler(
    WebsiteDbContext db,
    IKeycloakAdminService keycloakAdmin,
    IConfiguration configuration) : IRequestHandler<GetAdminSubscriptionQuery, OneOf<AdminSubscriptionDetailResponse, NotFound>>
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

        var nameMap = await ResolveUserNamesAsync(entity, cancellationToken).ConfigureAwait(false);

        return AdminSubscriptionMapper.ToDetailResponse(entity, nameMap);
    }

    private async Task<Dictionary<Guid, string>> ResolveUserNamesAsync(Domain.Entities.Subscription entity, CancellationToken ct)
    {
        var realm = configuration["Keycloak:AdminPortalRealm"] ?? "chairly-admin";
        var userIds = new HashSet<Guid>();

        if (entity.CreatedBy.HasValue)
        {
            userIds.Add(entity.CreatedBy.Value);
        }

        if (entity.ProvisionedBy.HasValue)
        {
            userIds.Add(entity.ProvisionedBy.Value);
        }

        if (entity.CancelledBy.HasValue)
        {
            userIds.Add(entity.CancelledBy.Value);
        }

        var nameMap = new Dictionary<Guid, string>();

        foreach (var userId in userIds)
        {
            var displayName = await keycloakAdmin.GetUserDisplayNameAsync(realm, userId.ToString(), ct).ConfigureAwait(false);
            if (displayName is not null)
            {
                nameMap[userId] = displayName;
            }
        }

        return nameMap;
    }
}
#pragma warning restore CA1812
