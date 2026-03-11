namespace Chairly.Domain.Entities;

public class TenantSettings
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string? CompanyName { get; set; }
    public string? CompanyEmail { get; set; }
    public string? Street { get; set; }
    public string? HouseNumber { get; set; }
    public string? PostalCode { get; set; }
    public string? City { get; set; }
    public string? CompanyPhone { get; set; }
    public string? IbanNumber { get; set; }
    public string? VatNumber { get; set; }
    public int? PaymentPeriodDays { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid? UpdatedBy { get; set; }
}
