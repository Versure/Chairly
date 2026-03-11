using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Tenancy;
using Chairly.Api.Shared.Validation;
using Chairly.Domain.Entities;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

#pragma warning disable CA1812
namespace Chairly.Api.Features.Settings.UpdateVatSettings;

internal sealed class UpdateVatSettingsHandler(ChairlyDbContext db) : IRequestHandler<UpdateVatSettingsCommand, VatSettingsResponse>
{
    public async Task<VatSettingsResponse> Handle(UpdateVatSettingsCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        VatRateValidator.ValidateRequired(command.DefaultVatRate);

        var vatSettings = await db.VatSettings
            .FirstOrDefaultAsync(v => v.TenantId == TenantConstants.DefaultTenantId, cancellationToken)
            .ConfigureAwait(false);

        if (vatSettings is null)
        {
            vatSettings = new VatSettings
            {
                Id = Guid.NewGuid(),
                TenantId = TenantConstants.DefaultTenantId,
                DefaultVatRate = command.DefaultVatRate,
                CreatedAtUtc = DateTimeOffset.UtcNow,
#pragma warning disable MA0026 // TODO: Replace with authenticated user ID from Keycloak (see Keycloak integration)
                CreatedBy = Guid.Empty,
#pragma warning restore MA0026
            };
            db.VatSettings.Add(vatSettings);
        }
        else
        {
            vatSettings.DefaultVatRate = command.DefaultVatRate;
            vatSettings.UpdatedAtUtc = DateTimeOffset.UtcNow;
#pragma warning disable MA0026 // TODO: Replace with authenticated user ID from Keycloak (see Keycloak integration)
            vatSettings.UpdatedBy = Guid.Empty;
#pragma warning restore MA0026
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new VatSettingsResponse(vatSettings.DefaultVatRate);
    }
}
#pragma warning restore CA1812
