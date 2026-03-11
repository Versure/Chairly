using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Tenancy;
using Chairly.Domain.Entities;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

#pragma warning disable CA1812
namespace Chairly.Api.Features.Settings.GetCompanyInfo;

internal sealed class GetCompanyInfoHandler(ChairlyDbContext db) : IRequestHandler<GetCompanyInfoQuery, CompanyInfoResponse>
{
    public async Task<CompanyInfoResponse> Handle(GetCompanyInfoQuery query, CancellationToken cancellationToken = default)
    {
        var settings = await db.TenantSettings
            .FirstOrDefaultAsync(s => s.TenantId == TenantConstants.DefaultTenantId, cancellationToken)
            .ConfigureAwait(false);

        if (settings is null)
        {
            settings = new TenantSettings
            {
                Id = Guid.NewGuid(),
                TenantId = TenantConstants.DefaultTenantId,
                CreatedAtUtc = DateTimeOffset.UtcNow,
#pragma warning disable MA0026 // TODO: Replace with authenticated user ID from Keycloak
                CreatedBy = Guid.Empty,
#pragma warning restore MA0026
            };

            db.TenantSettings.Add(settings);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return ToResponse(settings);
    }

    private static CompanyInfoResponse ToResponse(TenantSettings settings) =>
        new(
            settings.CompanyName,
            settings.CompanyEmail,
            settings.Street,
            settings.HouseNumber,
            settings.PostalCode,
            settings.City,
            settings.CompanyPhone,
            settings.IbanNumber,
            settings.VatNumber,
            settings.PaymentPeriodDays);
}
#pragma warning restore CA1812
