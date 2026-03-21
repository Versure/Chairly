namespace Chairly.Domain.Events;

public record SubscriptionCreatedEvent(
    Guid SubscriptionId,
    string SalonName,
    string OwnerFirstName,
    string OwnerLastName,
    string Email,
    string? PhoneNumber,
    string Plan,
    string? BillingCycle,
    bool IsTrial);
