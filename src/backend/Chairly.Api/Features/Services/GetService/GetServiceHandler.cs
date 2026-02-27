using Chairly.Api.Dispatching;
using Chairly.Api.Shared.Tenancy;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;

namespace Chairly.Api.Features.Services.GetService;

#pragma warning disable CA1812
internal sealed class GetServiceHandler(ChairlyDbContext db) : IRequestHandler<GetServiceQuery, OneOf<ServiceResponse, NotFound>>
{
    public async Task<OneOf<ServiceResponse, NotFound>> Handle(GetServiceQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var service = await db.Services
            .Include(s => s.Category)
            .FirstOrDefaultAsync(s => s.Id == query.Id && s.TenantId == TenantConstants.DefaultTenantId, cancellationToken)
            .ConfigureAwait(false);

        if (service is null)
        {
            return new NotFound();
        }

        return new ServiceResponse(
            service.Id,
            service.TenantId,
            service.Name,
            service.Description,
            service.Duration,
            service.Price,
            service.CategoryId,
            service.Category?.Name,
            service.IsActive,
            service.SortOrder,
            service.CreatedAtUtc,
            service.CreatedBy,
            service.UpdatedAtUtc,
            service.UpdatedBy);
    }
}
#pragma warning restore CA1812
