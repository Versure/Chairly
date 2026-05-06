namespace Chairly.Api.Features.Clients.GetClientTimeline;

internal sealed record ClientTimelineStatsResponse(
    int TotalVisits,
    DateTimeOffset? LastVisitAtUtc,
    decimal TotalSpentAmount,
    StaffMemberSummary? MostVisitedStaffMember,
    ServiceSummary? MostBookedService,
    int NoShowCount);
