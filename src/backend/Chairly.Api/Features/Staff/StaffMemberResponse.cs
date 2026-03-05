namespace Chairly.Api.Features.Staff;

internal sealed record StaffMemberResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string Role,
    string Color,
    string? PhotoUrl,
    bool IsActive,
    Dictionary<string, ShiftBlockResponse[]> Schedule,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);
