using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Tenancy;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;

namespace Chairly.Api.Features.Services.UpdateService;

#pragma warning disable CA1812
internal sealed class UpdateServiceHandler(ChairlyDbContext db) : IRequestHandler<UpdateServiceCommand, OneOf<ServiceResponse, NotFound>>
{
    public async Task<OneOf<ServiceResponse, NotFound>> Handle(UpdateServiceCommand command, CancellationToken cancellationToken = default)
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

        service.Name = command.Name;
        service.Description = command.Description;
        service.Duration = command.Duration;
        service.Price = command.Price;
        service.CategoryId = command.CategoryId;
        service.SortOrder = command.SortOrder;
        service.UpdatedAtUtc = DateTimeOffset.UtcNow;
#pragma warning disable MA0026 // TODO: Replace with authenticated user ID from Keycloak (see Keycloak integration)
        service.UpdatedBy = Guid.Empty;
#pragma warning restore MA0026

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        string? categoryName = null;
        if (service.CategoryId.HasValue)
        {
            categoryName = await db.ServiceCategories
                .Where(sc => sc.Id == service.CategoryId.Value)
                .Select(sc => sc.Name)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        return new ServiceResponse(
            service.Id,
            service.Name,
            service.Description,
            service.Duration,
            service.Price,
            service.CategoryId,
            categoryName,
            service.IsActive,
            service.SortOrder,
            service.CreatedAtUtc,
            service.CreatedBy,
            service.UpdatedAtUtc,
            service.UpdatedBy);
    }
}
#pragma warning restore CA1812
