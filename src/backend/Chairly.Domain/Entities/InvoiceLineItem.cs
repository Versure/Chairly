namespace Chairly.Domain.Entities;

public class InvoiceLineItem
{
    public Guid Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public decimal VatPercentage { get; set; }
    public decimal VatAmount { get; set; }
    public int SortOrder { get; set; }
    public bool IsManual { get; set; }
}
