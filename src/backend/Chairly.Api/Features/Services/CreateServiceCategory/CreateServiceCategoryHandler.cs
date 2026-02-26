using Chairly.Api.Dispatching;
using Chairly.Api.Shared.Tenancy;
using Chairly.Domain.Entities;
using Chairly.Infrastructure.Persistence;

namespace Chairly.Api.Features.Services.CreateServiceCategory;

#pragma warning disable CA1812
internal sealed class CreateServiceCategoryHandler(ChairlyDbContext db) : IRequestHandler<CreateServiceCategoryCommand, ServiceCategoryResponse>
{
    public async Task<ServiceCategoryResponse> Handle(CreateServiceCategoryCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var category = new ServiceCategory
        {
            Id = Guid.NewGuid(),
            TenantId = TenantConstants.DefaultTenantId,
            Name = command.Name,
            SortOrder = command.SortOrder,
        };

        db.ServiceCategories.Add(category);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new ServiceCategoryResponse(category.Id, category.TenantId, category.Name, category.SortOrder);
    }
}
#pragma warning restore CA1812
