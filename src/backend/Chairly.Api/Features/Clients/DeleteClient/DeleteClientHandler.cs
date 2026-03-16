using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Results;
using Chairly.Api.Shared.Tenancy;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;

namespace Chairly.Api.Features.Clients.DeleteClient;

#pragma warning disable CA1812
internal sealed class DeleteClientHandler(ChairlyDbContext db, ITenantContext tenantContext) : IRequestHandler<DeleteClientCommand, OneOf<Success, NotFound, Conflict>>
{
    public async Task<OneOf<Success, NotFound, Conflict>> Handle(DeleteClientCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var client = await db.Clients
            .FirstOrDefaultAsync(c => c.Id == command.Id && c.TenantId == tenantContext.TenantId, cancellationToken)
            .ConfigureAwait(false);

        if (client is null)
        {
            return new NotFound();
        }

        if (client.DeletedAtUtc != null)
        {
            return new Conflict();
        }

        client.DeletedAtUtc = DateTimeOffset.UtcNow;
        client.DeletedBy = tenantContext.UserId;

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new Success();
    }
}
#pragma warning restore CA1812
