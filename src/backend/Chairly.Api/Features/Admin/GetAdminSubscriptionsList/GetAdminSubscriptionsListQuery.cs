using System.ComponentModel.DataAnnotations;
using Chairly.Api.Shared.Mediator;

namespace Chairly.Api.Features.Admin.GetAdminSubscriptionsList;

internal sealed record GetAdminSubscriptionsListQuery(
    string? Search,
    string? Status,
    string? Plan,
    [property: Range(1, int.MaxValue)] int Page,
    [property: Range(1, 100)] int PageSize) : IRequest<AdminSubscriptionsListResponse>;
