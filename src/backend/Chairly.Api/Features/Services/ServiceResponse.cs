namespace Chairly.Api.Features.Services;

internal sealed record ServiceResponse(
    Guid Id,
    string Name,
    string? Description,
    TimeSpan Duration,
    decimal Price,
    decimal? VatRate,
    Guid? CategoryId,
    string? CategoryName,
    bool IsActive,
    int SortOrder,
    DateTimeOffset CreatedAtUtc,
    Guid CreatedBy,
    DateTimeOffset? UpdatedAtUtc,
    Guid? UpdatedBy);
