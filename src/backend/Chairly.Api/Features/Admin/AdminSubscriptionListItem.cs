namespace Chairly.Api.Features.Admin;

internal sealed record AdminSubscriptionListItem(
    Guid Id,
    string SalonName,
    string OwnerName,
    string Email,
    string Plan,
    string? BillingCycle,
    bool IsTrial,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ProvisionedAtUtc,
    DateTimeOffset? CancelledAtUtc);
