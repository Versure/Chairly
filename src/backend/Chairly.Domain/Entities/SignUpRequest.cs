namespace Chairly.Domain.Entities;

public class SignUpRequest
{
    public Guid Id { get; set; }
    public string SalonName { get; set; } = string.Empty;
    public string OwnerFirstName { get; set; } = string.Empty;
    public string OwnerLastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? ProvisionedAtUtc { get; set; }
    public Guid? ProvisionedBy { get; set; }
    public DateTimeOffset? RejectedAtUtc { get; set; }
    public Guid? RejectedBy { get; set; }
    public string? RejectionReason { get; set; }
}
