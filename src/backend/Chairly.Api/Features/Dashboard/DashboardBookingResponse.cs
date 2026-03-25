namespace Chairly.Api.Features.Dashboard;

internal sealed record DashboardBookingResponse(
    Guid Id,
    Guid ClientId,
    string ClientName,
    Guid StaffMemberId,
    string StaffMemberName,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    string Status,
    IReadOnlyList<string> ServiceNames);
