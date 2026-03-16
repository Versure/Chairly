using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Tenancy;
using Chairly.Domain.Entities;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

#pragma warning disable CA1812
namespace Chairly.Api.Features.Settings.GetVatSettings;

internal sealed class GetVatSettingsHandler(ChairlyDbContext db, ITenantContext tenantContext) : IRequestHandler<GetVatSettingsQuery, VatSettingsResponse>
{
    public async Task<VatSettingsResponse> Handle(GetVatSettingsQuery query, CancellationToken cancellationToken = default)
    {
        var vatSettings = await db.VatSettings
            .FirstOrDefaultAsync(v => v.TenantId == tenantContext.TenantId, cancellationToken)
            .ConfigureAwait(false);

        if (vatSettings is null)
        {
            vatSettings = new VatSettings
            {
                Id = Guid.NewGuid(),
                TenantId = tenantContext.TenantId,
                DefaultVatRate = 21m,
                CreatedAtUtc = DateTimeOffset.UtcNow,
                CreatedBy = tenantContext.UserId,
            };
            db.VatSettings.Add(vatSettings);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return new VatSettingsResponse(vatSettings.DefaultVatRate);
    }
}
#pragma warning restore CA1812
