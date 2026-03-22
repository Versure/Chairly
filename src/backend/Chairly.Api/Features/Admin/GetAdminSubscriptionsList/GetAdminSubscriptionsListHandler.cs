using Chairly.Api.Shared.Mediator;
using Chairly.Domain.Enums;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

#pragma warning disable CA1812
namespace Chairly.Api.Features.Admin.GetAdminSubscriptionsList;

internal sealed class GetAdminSubscriptionsListHandler(WebsiteDbContext db) : IRequestHandler<GetAdminSubscriptionsListQuery, AdminSubscriptionsListResponse>
{
    public async Task<AdminSubscriptionsListResponse> Handle(GetAdminSubscriptionsListQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var queryable = db.Subscriptions.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
#pragma warning disable CA1862 // EF Core Npgsql translates StringComparison.OrdinalIgnoreCase to ILIKE
            queryable = queryable.Where(s =>
                s.SalonName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                s.Email.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                s.OwnerFirstName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                s.OwnerLastName.Contains(search, StringComparison.OrdinalIgnoreCase));
#pragma warning restore CA1862
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            queryable = query.Status.ToUpperInvariant() switch
            {
                "PENDING" => queryable.Where(s => s.TrialEndsAtUtc == null && s.ProvisionedAtUtc == null && s.CancelledAtUtc == null),
                "TRIAL" => queryable.Where(s => s.TrialEndsAtUtc != null && s.ProvisionedAtUtc == null && s.CancelledAtUtc == null),
                "PROVISIONED" => queryable.Where(s => s.ProvisionedAtUtc != null && s.CancelledAtUtc == null),
                "CANCELLED" => queryable.Where(s => s.CancelledAtUtc != null),
                _ => queryable,
            };
        }

        if (!string.IsNullOrWhiteSpace(query.Plan) && Enum.TryParse<SubscriptionPlan>(query.Plan, ignoreCase: true, out var plan))
        {
            queryable = queryable.Where(s => s.Plan == plan);
        }

        var totalCount = await queryable.CountAsync(cancellationToken).ConfigureAwait(false);

        var entities = await queryable
            .OrderByDescending(s => s.CreatedAtUtc)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var items = entities.ConvertAll(AdminSubscriptionMapper.ToListItem);

        return new AdminSubscriptionsListResponse(items, totalCount, query.Page, query.PageSize);
    }
}
#pragma warning restore CA1812
