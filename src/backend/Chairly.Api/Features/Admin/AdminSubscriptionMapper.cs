using Chairly.Domain.Entities;
using Chairly.Domain.Enums;

namespace Chairly.Api.Features.Admin;

internal static class AdminSubscriptionMapper
{
    private static readonly Dictionary<SubscriptionPlan, string> _planSlugs = new()
    {
        [SubscriptionPlan.Starter] = "starter",
        [SubscriptionPlan.Team] = "team",
        [SubscriptionPlan.Salon] = "salon",
    };

    internal static string DeriveStatus(Subscription s) =>
        s.CancelledAtUtc is not null ? "cancelled" :
        s.ProvisionedAtUtc is not null ? "provisioned" :
        s.TrialEndsAtUtc is not null ? "trial" :
        "pending";

    internal static string GetPlanSlug(SubscriptionPlan plan) =>
        _planSlugs[plan];

    internal static AdminSubscriptionDetailResponse ToDetailResponse(Subscription entity) =>
        new(
            entity.Id,
            entity.SalonName,
            entity.OwnerFirstName,
            entity.OwnerLastName,
            entity.Email,
            entity.PhoneNumber,
            GetPlanSlug(entity.Plan),
            entity.BillingCycle?.ToString(),
            entity.IsTrial,
            DeriveStatus(entity),
            entity.TrialEndsAtUtc,
            entity.CreatedAtUtc,
            entity.CreatedBy,
            entity.ProvisionedAtUtc,
            entity.ProvisionedBy,
            entity.CancelledAtUtc,
            entity.CancelledBy,
            entity.CancellationReason);

    internal static AdminSubscriptionListItem ToListItem(Subscription entity) =>
        new(
            entity.Id,
            entity.SalonName,
            $"{entity.OwnerFirstName} {entity.OwnerLastName}",
            entity.Email,
            GetPlanSlug(entity.Plan),
            entity.BillingCycle?.ToString(),
            entity.IsTrial,
            DeriveStatus(entity),
            entity.CreatedAtUtc,
            entity.ProvisionedAtUtc,
            entity.CancelledAtUtc);
}
