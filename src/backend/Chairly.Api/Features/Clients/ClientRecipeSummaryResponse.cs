namespace Chairly.Api.Features.Clients;

internal sealed record ClientRecipeSummaryResponse(
    Guid Id,
    Guid BookingId,
    DateTimeOffset BookingDate,
    Guid StaffMemberId,
    string StaffMemberName,
    string Title,
    string? Notes,
    IReadOnlyList<RecipeProductResponse> Products,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);
