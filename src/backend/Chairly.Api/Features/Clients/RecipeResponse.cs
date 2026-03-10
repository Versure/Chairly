namespace Chairly.Api.Features.Clients;

internal sealed record RecipeResponse(
    Guid Id,
    Guid BookingId,
    Guid ClientId,
    Guid StaffMemberId,
    string Title,
    string? Notes,
    IReadOnlyList<RecipeProductResponse> Products,
    DateTimeOffset CreatedAtUtc,
    Guid CreatedBy,
    DateTimeOffset? UpdatedAtUtc,
    Guid? UpdatedBy);
