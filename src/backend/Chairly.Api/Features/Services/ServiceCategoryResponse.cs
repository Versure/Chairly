namespace Chairly.Api.Features.Services;

internal sealed record ServiceCategoryResponse(
    Guid Id,
    Guid TenantId,
    string Name,
    int SortOrder,
    DateTimeOffset CreatedAtUtc,
    Guid CreatedBy);
