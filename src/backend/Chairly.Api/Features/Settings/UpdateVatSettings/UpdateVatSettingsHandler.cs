using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Tenancy;
using Chairly.Api.Shared.Validation;
using Chairly.Domain.Entities;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

#pragma warning disable CA1812
namespace Chairly.Api.Features.Settings.UpdateVatSettings;

internal sealed class UpdateVatSettingsHandler(ChairlyDbContext db, ITenantContext tenantContext) : IRequestHandler<UpdateVatSettingsCommand, VatSettingsResponse>
{
    public async Task<VatSettingsResponse> Handle(UpdateVatSettingsCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        VatRateValidator.ValidateRequired(command.DefaultVatRate);

        var vatSettings = await db.VatSettings
            .FirstOrDefaultAsync(v => v.TenantId == tenantContext.TenantId, cancellationToken)
            .ConfigureAwait(false);

        if (vatSettings is null)
        {
            vatSettings = new VatSettings
            {
                Id = Guid.NewGuid(),
                TenantId = tenantContext.TenantId,
                DefaultVatRate = command.DefaultVatRate,
                CreatedAtUtc = DateTimeOffset.UtcNow,
                CreatedBy = tenantContext.UserId,
            };
            db.VatSettings.Add(vatSettings);
        }
        else
        {
            vatSettings.DefaultVatRate = command.DefaultVatRate;
            vatSettings.UpdatedAtUtc = DateTimeOffset.UtcNow;
            vatSettings.UpdatedBy = tenantContext.UserId;
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new VatSettingsResponse(vatSettings.DefaultVatRate);
    }
}
#pragma warning restore CA1812
