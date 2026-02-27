using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Tenancy;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;

namespace Chairly.Api.Features.Services.ToggleServiceActive;

#pragma warning disable CA1812
internal sealed class ToggleServiceActiveHandler(ChairlyDbContext db) : IRequestHandler<ToggleServiceActiveCommand, OneOf<ServiceResponse, NotFound>>
{
    public async Task<OneOf<ServiceResponse, NotFound>> Handle(ToggleServiceActiveCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var service = await db.Services
            .Include(s => s.Category)
            .FirstOrDefaultAsync(s => s.Id == command.Id && s.TenantId == TenantConstants.DefaultTenantId, cancellationToken)
            .ConfigureAwait(false);

        if (service is null)
        {
            return new NotFound();
        }

        service.IsActive = !service.IsActive;
        service.UpdatedAtUtc = DateTimeOffset.UtcNow;
#pragma warning disable MA0026 // TODO: Replace with authenticated user ID from Keycloak (see Keycloak integration)
        service.UpdatedBy = Guid.Empty;
#pragma warning restore MA0026

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new ServiceResponse(
            service.Id,
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
