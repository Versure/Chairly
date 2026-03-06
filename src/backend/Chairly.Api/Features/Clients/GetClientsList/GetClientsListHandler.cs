using Chairly.Api.Features.Clients.CreateClient;
using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Tenancy;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Chairly.Api.Features.Clients.GetClientsList;

#pragma warning disable CA1812
internal sealed class GetClientsListHandler(ChairlyDbContext db) : IRequestHandler<GetClientsListQuery, IEnumerable<ClientResponse>>
{
    public async Task<IEnumerable<ClientResponse>> Handle(GetClientsListQuery query, CancellationToken cancellationToken = default)
    {
        var clients = await db.Clients
            .Where(c => c.TenantId == TenantConstants.DefaultTenantId && c.DeletedAtUtc == null)
            .OrderBy(c => c.LastName)
            .ThenBy(c => c.FirstName)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return clients.Select(CreateClientHandler.ToResponse);
    }
}
#pragma warning restore CA1812
