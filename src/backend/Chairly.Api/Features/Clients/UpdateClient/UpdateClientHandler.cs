using Chairly.Api.Features.Clients.CreateClient;
using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Tenancy;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;

namespace Chairly.Api.Features.Clients.UpdateClient;

#pragma warning disable CA1812
internal sealed class UpdateClientHandler(ChairlyDbContext db, ITenantContext tenantContext) : IRequestHandler<UpdateClientCommand, OneOf<ClientResponse, NotFound>>
{
    public async Task<OneOf<ClientResponse, NotFound>> Handle(UpdateClientCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var client = await db.Clients
            .FirstOrDefaultAsync(c => c.Id == command.Id && c.TenantId == tenantContext.TenantId && c.DeletedAtUtc == null, cancellationToken)
            .ConfigureAwait(false);

        if (client is null)
        {
            return new NotFound();
        }

        client.FirstName = command.FirstName;
        client.LastName = command.LastName;
        client.Email = command.Email;
        client.PhoneNumber = command.PhoneNumber;
        client.Notes = command.Notes;
        client.UpdatedAtUtc = DateTimeOffset.UtcNow;
        client.UpdatedBy = tenantContext.UserId;

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return CreateClientHandler.ToResponse(client);
    }
}
#pragma warning restore CA1812
