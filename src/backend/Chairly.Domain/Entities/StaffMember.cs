using Chairly.Domain.Enums;

#pragma warning disable CA1056 // PhotoUrl stored as string for EF Core compatibility
namespace Chairly.Domain.Entities;

public class StaffMember
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? KeycloakUserId { get; set; }
    public StaffRole Role { get; set; }
    public string Color { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; } // Stored as string for EF Core compatibility
#pragma warning restore CA1056
    public string ScheduleJson { get; set; } = "{}";
    public DateTimeOffset? DeactivatedAtUtc { get; set; }
    public Guid? DeactivatedBy { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid? UpdatedBy { get; set; }
}
