using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Tenancy;
using Chairly.Domain.Entities;
using Chairly.Infrastructure.Persistence;

namespace Chairly.Api.Features.Services.CreateServiceCategory;

#pragma warning disable CA1812
internal sealed class CreateServiceCategoryHandler(ChairlyDbContext db, ITenantContext tenantContext) : IRequestHandler<CreateServiceCategoryCommand, ServiceCategoryResponse>
{
    public async Task<ServiceCategoryResponse> Handle(CreateServiceCategoryCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var category = new ServiceCategory
        {
            Id = Guid.NewGuid(),
            TenantId = tenantContext.TenantId,
            Name = command.Name,
            SortOrder = command.SortOrder,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedBy = tenantContext.UserId,
        };

        db.ServiceCategories.Add(category);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new ServiceCategoryResponse(category.Id, category.Name, category.SortOrder, category.CreatedAtUtc, category.CreatedBy);
    }
}
#pragma warning restore CA1812
