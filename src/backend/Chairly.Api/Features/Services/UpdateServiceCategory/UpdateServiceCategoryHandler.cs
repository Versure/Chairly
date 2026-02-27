using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Tenancy;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;

namespace Chairly.Api.Features.Services.UpdateServiceCategory;

#pragma warning disable CA1812
internal sealed class UpdateServiceCategoryHandler(ChairlyDbContext db) : IRequestHandler<UpdateServiceCategoryCommand, OneOf<ServiceCategoryResponse, NotFound>>
{
    public async Task<OneOf<ServiceCategoryResponse, NotFound>> Handle(UpdateServiceCategoryCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var category = await db.ServiceCategories
            .FirstOrDefaultAsync(sc => sc.Id == command.Id && sc.TenantId == TenantConstants.DefaultTenantId, cancellationToken)
            .ConfigureAwait(false);

        if (category is null)
        {
            return new NotFound();
        }

        category.Name = command.Name;
        category.SortOrder = command.SortOrder;

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new ServiceCategoryResponse(category.Id, category.Name, category.SortOrder, category.CreatedAtUtc, category.CreatedBy);
    }
}
#pragma warning restore CA1812
