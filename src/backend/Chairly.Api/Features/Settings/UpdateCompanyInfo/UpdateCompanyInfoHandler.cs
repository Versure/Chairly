using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Results;
using Chairly.Api.Shared.Tenancy;
using Chairly.Domain.Entities;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using OneOf;

#pragma warning disable CA1812
namespace Chairly.Api.Features.Settings.UpdateCompanyInfo;

internal sealed class UpdateCompanyInfoHandler(ChairlyDbContext db) : IRequestHandler<UpdateCompanyInfoCommand, OneOf<CompanyInfoResponse, Forbidden>>
{
    public async Task<OneOf<CompanyInfoResponse, Forbidden>> Handle(UpdateCompanyInfoCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

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
        }

        settings.CompanyName = command.CompanyName;
        settings.CompanyEmail = command.CompanyEmail;
        settings.CompanyAddress = command.CompanyAddress;
        settings.CompanyPhone = command.CompanyPhone;
        settings.IbanNumber = command.IbanNumber;
        settings.VatNumber = command.VatNumber;
        settings.PaymentPeriodDays = command.PaymentPeriodDays;
        settings.UpdatedAtUtc = DateTimeOffset.UtcNow;
#pragma warning disable MA0026 // TODO: Replace with authenticated user ID from Keycloak
        settings.UpdatedBy = Guid.Empty;
#pragma warning restore MA0026

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return ToResponse(settings);
    }

    private static CompanyInfoResponse ToResponse(TenantSettings settings) =>
        new(
            settings.CompanyName,
            settings.CompanyEmail,
            settings.CompanyAddress,
            settings.CompanyPhone,
            settings.IbanNumber,
            settings.VatNumber,
            settings.PaymentPeriodDays);
}
#pragma warning restore CA1812
