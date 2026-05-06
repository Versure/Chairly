namespace Chairly.Api.Features.Clients.GetClientTimeline;

internal sealed record ClientTimelineResponse(
    ClientResponse Profile,
    ClientTimelineStatsResponse Stats,
    IReadOnlyList<TimelineEntryResponse> Timeline);
