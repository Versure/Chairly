using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Results;
using Chairly.Domain.Enums;
using Chairly.Infrastructure.Keycloak;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OneOf;
using OneOf.Types;

#pragma warning disable CA1812
namespace Chairly.Api.Features.Admin.UpdateSubscriptionPlan;

internal sealed class UpdateSubscriptionPlanHandler(
    WebsiteDbContext db,
    IKeycloakAdminService keycloakAdmin,
    IConfiguration configuration) : IRequestHandler<UpdateSubscriptionPlanCommand, OneOf<AdminSubscriptionDetailResponse, NotFound, Unprocessable>>
{
    public async Task<OneOf<AdminSubscriptionDetailResponse, NotFound, Unprocessable>> Handle(UpdateSubscriptionPlanCommand command, CancellationToken cancellationToken = default)
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
            return new Unprocessable("Geannuleerd abonnement kan niet worden bijgewerkt.");
        }

        if (!Enum.TryParse<SubscriptionPlan>(command.Plan, ignoreCase: true, out var plan))
        {
            return new Unprocessable($"Ongeldig plan: '{command.Plan}'. Geldige waarden: starter, team, salon.");
        }

        BillingCycle? billingCycle = null;
        if (command.BillingCycle is not null)
        {
            if (!Enum.TryParse<BillingCycle>(command.BillingCycle, ignoreCase: true, out var parsedCycle))
            {
                return new Unprocessable($"Ongeldige factuurperiode: '{command.BillingCycle}'. Geldige waarden: Monthly, Annual.");
            }

            billingCycle = parsedCycle;
        }

        // Trial to paid conversion: if subscription is a trial and billingCycle is provided, clear TrialEndsAtUtc.
        if (entity.TrialEndsAtUtc is not null && billingCycle is not null)
        {
            entity.TrialEndsAtUtc = null;
        }

        // Paid subscription must have a billing cycle.
        if (entity.TrialEndsAtUtc is null && billingCycle is null)
        {
            return new Unprocessable("Betaald abonnement vereist een factuurperiode.");
        }

        entity.Plan = plan;
        if (billingCycle is not null)
        {
            entity.BillingCycle = billingCycle;
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

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
