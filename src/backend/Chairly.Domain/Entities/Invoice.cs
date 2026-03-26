using Chairly.Domain.Enums;

namespace Chairly.Domain.Entities;

public class Invoice
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid BookingId { get; set; }
    public Guid ClientId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateOnly InvoiceDate { get; set; }
    public decimal SubTotalAmount { get; set; }
    public decimal TotalVatAmount { get; set; }
    public decimal TotalAmount { get; set; }

#pragma warning disable CA1002, CA2227, MA0016 // EF Core requires mutable collection for navigation property
    public List<InvoiceLineItem> LineItems { get; set; } = [];
#pragma warning restore CA1002, CA2227, MA0016

    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset? SentAtUtc { get; set; }
    public Guid? SentBy { get; set; }
    public DateTimeOffset? PaidAtUtc { get; set; }
    public Guid? PaidBy { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public DateTimeOffset? VoidedAtUtc { get; set; }
    public Guid? VoidedBy { get; set; }
}
