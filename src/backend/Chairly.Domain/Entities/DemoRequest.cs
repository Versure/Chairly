namespace Chairly.Domain.Entities;

public class DemoRequest
{
    public Guid Id { get; set; }
    public string ContactName { get; set; } = string.Empty;
    public string SalonName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Message { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? ReviewedAtUtc { get; set; }
    public Guid? ReviewedBy { get; set; }
}
