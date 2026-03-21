namespace Chairly.Api.Features.Onboarding;

internal sealed record SubscriptionResponse(
    Guid Id,
    string SalonName,
    string OwnerFirstName,
    string OwnerLastName,
    string Email,
    string Plan,
    string? BillingCycle,
    bool IsTrial,
    DateTimeOffset? TrialEndsAtUtc,
    DateTimeOffset CreatedAtUtc);
