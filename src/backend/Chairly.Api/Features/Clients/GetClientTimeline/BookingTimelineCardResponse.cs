namespace Chairly.Api.Features.Clients.GetClientTimeline;

internal sealed record BookingTimelineCardResponse(
    Guid Id,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    int DurationMinutes,
    string Status,
    Guid StaffMemberId,
    string StaffMemberName,
    decimal TotalPrice,
    string? Notes,
    IReadOnlyList<BookingTimelineServiceResponse> Services,
    DateTimeOffset? ConfirmedAtUtc,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    DateTimeOffset? CancelledAtUtc,
    DateTimeOffset? NoShowAtUtc);
