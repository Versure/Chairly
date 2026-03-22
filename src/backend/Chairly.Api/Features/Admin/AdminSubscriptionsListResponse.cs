namespace Chairly.Api.Features.Admin;

internal sealed record AdminSubscriptionsListResponse(
    IReadOnlyList<AdminSubscriptionListItem> Items,
    int TotalCount,
    int Page,
    int PageSize);
