namespace Chairly.Domain.Entities;

public class BookingService
{
    public Guid Id { get; set; }
    public Guid ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public decimal Price { get; set; }
    public int SortOrder { get; set; }
}
