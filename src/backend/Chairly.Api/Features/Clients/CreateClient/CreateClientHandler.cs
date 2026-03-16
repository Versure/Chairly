using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Tenancy;
using Chairly.Domain.Entities;
using Chairly.Infrastructure.Persistence;

namespace Chairly.Api.Features.Clients.CreateClient;

#pragma warning disable CA1812
internal sealed class CreateClientHandler(ChairlyDbContext db, ITenantContext tenantContext) : IRequestHandler<CreateClientCommand, ClientResponse>
{
    public async Task<ClientResponse> Handle(CreateClientCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var client = new Client
        {
            Id = Guid.NewGuid(),
            TenantId = tenantContext.TenantId,
            FirstName = command.FirstName,
            LastName = command.LastName,
            Email = command.Email,
            PhoneNumber = command.PhoneNumber,
            Notes = command.Notes,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedBy = tenantContext.UserId,
        };

        db.Clients.Add(client);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return ToResponse(client);
    }

    internal static ClientResponse ToResponse(Client client) => new(
        client.Id,
        client.FirstName,
        client.LastName,
        client.Email,
        client.PhoneNumber,
        client.Notes,
        client.CreatedAtUtc,
        client.UpdatedAtUtc);
}
#pragma warning restore CA1812
