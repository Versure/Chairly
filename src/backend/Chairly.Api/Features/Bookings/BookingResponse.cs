namespace Chairly.Api.Features.Bookings;

internal sealed record BookingResponse(
    Guid Id,
    Guid ClientId,
    Guid StaffMemberId,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    string? Notes,
    string Status,
    IReadOnlyList<BookingServiceResponse> Services,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc,
    DateTimeOffset? ConfirmedAtUtc,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    DateTimeOffset? CancelledAtUtc,
    DateTimeOffset? NoShowAtUtc);
