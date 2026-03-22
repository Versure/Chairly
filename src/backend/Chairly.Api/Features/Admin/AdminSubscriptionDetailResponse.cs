namespace Chairly.Api.Features.Admin;

internal sealed record AdminSubscriptionDetailResponse(
    Guid Id,
    string SalonName,
    string OwnerFirstName,
    string OwnerLastName,
    string Email,
    string? PhoneNumber,
    string Plan,
    string? BillingCycle,
    bool IsTrial,
    string Status,
    DateTimeOffset? TrialEndsAtUtc,
    DateTimeOffset CreatedAtUtc,
    Guid? CreatedBy,
    DateTimeOffset? ProvisionedAtUtc,
    Guid? ProvisionedBy,
    DateTimeOffset? CancelledAtUtc,
    Guid? CancelledBy,
    string? CancellationReason);
