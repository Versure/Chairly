using Chairly.Api.Dispatching;
using Chairly.Api.Shared.Tenancy;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Chairly.Api.Features.Services.GetServicesList;

#pragma warning disable CA1812
internal sealed class GetServicesListHandler(ChairlyDbContext db) : IRequestHandler<GetServicesListQuery, IEnumerable<ServiceResponse>>
{
    public async Task<IEnumerable<ServiceResponse>> Handle(GetServicesListQuery query, CancellationToken cancellationToken = default)
    {
        return await db.Services
            .Include(s => s.Category)
            .Where(s => s.TenantId == TenantConstants.DefaultTenantId)
            .OrderBy(s => s.SortOrder)
            .Select(s => new ServiceResponse(
                s.Id,
                s.TenantId,
                s.Name,
                s.Description,
                s.Duration,
                s.Price,
                s.CategoryId,
                s.Category != null ? s.Category.Name : null,
                s.IsActive,
                s.SortOrder,
                s.CreatedAtUtc,
                s.UpdatedAtUtc))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
#pragma warning restore CA1812
