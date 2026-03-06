namespace Chairly.Domain.Entities;

public class Booking
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ClientId { get; set; }
    public Guid StaffMemberId { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid? UpdatedBy { get; set; }
    public DateTimeOffset? ConfirmedAtUtc { get; set; }
    public Guid? ConfirmedBy { get; set; }
    public DateTimeOffset? StartedAtUtc { get; set; }
    public Guid? StartedBy { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public Guid? CompletedBy { get; set; }
    public DateTimeOffset? CancelledAtUtc { get; set; }
    public Guid? CancelledBy { get; set; }
    public DateTimeOffset? NoShowAtUtc { get; set; }
    public Guid? NoShowBy { get; set; }

    public Client? Client { get; set; }
    public StaffMember? StaffMember { get; set; }

#pragma warning disable CA1002, CA2227, MA0016 // EF Core requires mutable collection for navigation property
    public List<BookingService> BookingServices { get; set; } = [];
#pragma warning restore CA1002, CA2227, MA0016
}
