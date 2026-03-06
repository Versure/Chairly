namespace Chairly.Api.Features.Bookings;

internal sealed record BookingServiceResponse(
    Guid ServiceId,
    string ServiceName,
    TimeSpan Duration,
    decimal Price,
    int SortOrder);
