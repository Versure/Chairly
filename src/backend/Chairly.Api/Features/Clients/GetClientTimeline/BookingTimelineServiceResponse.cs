namespace Chairly.Api.Features.Clients.GetClientTimeline;

internal sealed record BookingTimelineServiceResponse(
    Guid ServiceId,
    string ServiceName,
    int DurationMinutes,
    decimal Price,
    int SortOrder);
