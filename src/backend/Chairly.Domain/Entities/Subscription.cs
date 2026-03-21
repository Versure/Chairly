using Chairly.Domain.Enums;

namespace Chairly.Domain.Entities;

public class Subscription
{
    public Guid Id { get; set; }
    public string SalonName { get; set; } = string.Empty;
    public string OwnerFirstName { get; set; } = string.Empty;
    public string OwnerLastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public SubscriptionPlan Plan { get; set; }
    public BillingCycle? BillingCycle { get; set; }
    public DateTimeOffset? TrialEndsAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? ProvisionedAtUtc { get; set; }
    public Guid? ProvisionedBy { get; set; }
    public DateTimeOffset? CancelledAtUtc { get; set; }
    public Guid? CancelledBy { get; set; }
    public string? CancellationReason { get; set; }

    // Derived property -- not persisted
    public bool IsTrial => TrialEndsAtUtc is not null;
}
