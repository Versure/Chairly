using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Tenancy;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Chairly.Api.Features.Services.GetServicesList;

#pragma warning disable CA1812
internal sealed class GetServicesListHandler(ChairlyDbContext db, ITenantContext tenantContext) : IRequestHandler<GetServicesListQuery, IEnumerable<ServiceResponse>>
{
    public async Task<IEnumerable<ServiceResponse>> Handle(GetServicesListQuery query, CancellationToken cancellationToken = default)
    {
        return await db.Services
            .Include(s => s.Category)
            .Where(s => s.TenantId == tenantContext.TenantId)
            .OrderBy(s => s.SortOrder)
            .Select(s => new ServiceResponse(
                s.Id,
                s.Name,
                s.Description,
                s.Duration,
                s.Price,
                s.VatRate,
                s.CategoryId,
                s.Category != null ? s.Category.Name : null,
                s.IsActive,
                s.SortOrder,
                s.CreatedAtUtc,
                s.CreatedBy,
                s.UpdatedAtUtc,
                s.UpdatedBy))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
#pragma warning restore CA1812
