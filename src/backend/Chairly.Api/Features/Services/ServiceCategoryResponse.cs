namespace Chairly.Api.Features.Services;

internal sealed record ServiceCategoryResponse(
    Guid Id,
    string Name,
    int SortOrder,
    DateTimeOffset CreatedAtUtc,
    Guid CreatedBy);
