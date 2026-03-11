using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Tenancy;
using Chairly.Domain.Entities;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

#pragma warning disable CA1812
namespace Chairly.Api.Features.Settings.GetVatSettings;

internal sealed class GetVatSettingsHandler(ChairlyDbContext db) : IRequestHandler<GetVatSettingsQuery, VatSettingsResponse>
{
    public async Task<VatSettingsResponse> Handle(GetVatSettingsQuery query, CancellationToken cancellationToken = default)
    {
        var vatSettings = await db.VatSettings
            .FirstOrDefaultAsync(v => v.TenantId == TenantConstants.DefaultTenantId, cancellationToken)
            .ConfigureAwait(false);

        if (vatSettings is null)
        {
            vatSettings = new VatSettings
            {
                Id = Guid.NewGuid(),
                TenantId = TenantConstants.DefaultTenantId,
                DefaultVatRate = 21m,
                CreatedAtUtc = DateTimeOffset.UtcNow,
#pragma warning disable MA0026 // TODO: Replace with authenticated user ID from Keycloak (see Keycloak integration)
                CreatedBy = Guid.Empty,
#pragma warning restore MA0026
            };
            db.VatSettings.Add(vatSettings);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return new VatSettingsResponse(vatSettings.DefaultVatRate);
    }
}
#pragma warning restore CA1812
