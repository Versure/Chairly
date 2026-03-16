using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Results;
using Chairly.Api.Shared.Tenancy;
using Chairly.Domain.Entities;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using OneOf;

#pragma warning disable CA1812
namespace Chairly.Api.Features.Settings.UpdateCompanyInfo;

internal sealed class UpdateCompanyInfoHandler(ChairlyDbContext db, ITenantContext tenantContext) : IRequestHandler<UpdateCompanyInfoCommand, OneOf<CompanyInfoResponse, Forbidden>>
{
    public async Task<OneOf<CompanyInfoResponse, Forbidden>> Handle(UpdateCompanyInfoCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var settings = await db.TenantSettings
            .FirstOrDefaultAsync(s => s.TenantId == tenantContext.TenantId, cancellationToken)
            .ConfigureAwait(false);

        if (settings is null)
        {
            settings = new TenantSettings
            {
                Id = Guid.NewGuid(),
                TenantId = tenantContext.TenantId,
                CreatedAtUtc = DateTimeOffset.UtcNow,
                CreatedBy = tenantContext.UserId,
            };

            db.TenantSettings.Add(settings);
        }

        settings.CompanyName = command.CompanyName;
        settings.CompanyEmail = command.CompanyEmail;
        settings.Street = command.Street;
        settings.HouseNumber = command.HouseNumber;
        settings.PostalCode = command.PostalCode;
        settings.City = command.City;
        settings.CompanyPhone = command.CompanyPhone;
        settings.IbanNumber = command.IbanNumber;
        settings.VatNumber = command.VatNumber;
        settings.PaymentPeriodDays = command.PaymentPeriodDays;
        settings.UpdatedAtUtc = DateTimeOffset.UtcNow;
        settings.UpdatedBy = tenantContext.UserId;

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

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
