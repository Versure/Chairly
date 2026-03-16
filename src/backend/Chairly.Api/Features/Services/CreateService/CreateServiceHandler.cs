using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Tenancy;
using Chairly.Api.Shared.Validation;
using Chairly.Domain.Entities;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Chairly.Api.Features.Services.CreateService;

#pragma warning disable CA1812
internal sealed class CreateServiceHandler(ChairlyDbContext db, ITenantContext tenantContext) : IRequestHandler<CreateServiceCommand, ServiceResponse>
{
    public async Task<ServiceResponse> Handle(CreateServiceCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        VatRateValidator.Validate(command.VatRate);

        var service = new Service
        {
            Id = Guid.NewGuid(),
            TenantId = tenantContext.TenantId,
            Name = command.Name,
            Description = command.Description,
            Duration = command.Duration,
            Price = command.Price,
            VatRate = command.VatRate,
            CategoryId = command.CategoryId,
            IsActive = true,
            SortOrder = command.SortOrder,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedBy = tenantContext.UserId,
        };

        db.Services.Add(service);
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

        return ToResponse(service, categoryName);
    }

    private static ServiceResponse ToResponse(Service service, string? categoryName) =>
        new(
            service.Id,
            service.Name,
            service.Description,
            service.Duration,
            service.Price,
            service.VatRate,
            service.CategoryId,
            categoryName,
            service.IsActive,
            service.SortOrder,
            service.CreatedAtUtc,
            service.CreatedBy,
            service.UpdatedAtUtc,
            service.UpdatedBy);
}
#pragma warning restore CA1812
