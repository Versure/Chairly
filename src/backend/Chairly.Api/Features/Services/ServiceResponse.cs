namespace Chairly.Api.Features.Services;

internal sealed record ServiceResponse(
    Guid Id,
    Guid TenantId,
    string Name,
    string? Description,
    TimeSpan Duration,
    decimal Price,
    Guid? CategoryId,
    string? CategoryName,
    bool IsActive,
    int SortOrder,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);
