using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Tenancy;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;

namespace Chairly.Api.Features.Services.DeleteServiceCategory;

#pragma warning disable CA1812
internal sealed class DeleteServiceCategoryHandler(ChairlyDbContext db, ITenantContext tenantContext) : IRequestHandler<DeleteServiceCategoryCommand, OneOf<Success, NotFound>>
{
    public async Task<OneOf<Success, NotFound>> Handle(DeleteServiceCategoryCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var category = await db.ServiceCategories
            .FirstOrDefaultAsync(sc => sc.Id == command.Id && sc.TenantId == tenantContext.TenantId, cancellationToken)
            .ConfigureAwait(false);

        if (category is null)
        {
            return new NotFound();
        }

        db.ServiceCategories.Remove(category);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new Success();
    }
}
#pragma warning restore CA1812
