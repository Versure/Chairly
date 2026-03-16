using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Tenancy;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Chairly.Api.Features.Services.GetServiceCategoriesList;

#pragma warning disable CA1812
internal sealed class GetServiceCategoriesListHandler(ChairlyDbContext db, ITenantContext tenantContext) : IRequestHandler<GetServiceCategoriesListQuery, IEnumerable<ServiceCategoryResponse>>
{
    public async Task<IEnumerable<ServiceCategoryResponse>> Handle(GetServiceCategoriesListQuery query, CancellationToken cancellationToken = default)
    {
        return await db.ServiceCategories
            .Where(sc => sc.TenantId == tenantContext.TenantId)
            .OrderBy(sc => sc.SortOrder)
            .Select(sc => new ServiceCategoryResponse(sc.Id, sc.Name, sc.SortOrder, sc.CreatedAtUtc, sc.CreatedBy))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
#pragma warning restore CA1812
