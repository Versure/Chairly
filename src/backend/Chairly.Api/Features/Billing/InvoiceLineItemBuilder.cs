using Chairly.Api.Shared.Tenancy;
using Chairly.Domain.Entities;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

#pragma warning disable CA1812
namespace Chairly.Api.Features.Billing;

internal sealed class InvoiceLineItemBuilder(ChairlyDbContext db)
{
    public async Task<List<InvoiceLineItem>> BuildFromBookingAsync(
        IEnumerable<BookingService> bookingServices,
        CancellationToken cancellationToken)
    {
        var vatSettings = await ResolveVatSettingsAsync(cancellationToken).ConfigureAwait(false);

        var serviceIds = bookingServices.Select(bs => bs.ServiceId).Distinct().ToList();
        var services = await db.Services
            .Where(s => serviceIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, cancellationToken)
            .ConfigureAwait(false);

        return BuildLineItems(bookingServices, services, vatSettings.DefaultVatRate);
    }

    internal async Task<VatSettings> ResolveVatSettingsAsync(CancellationToken cancellationToken)
    {
        var vatSettings = await db.VatSettings
            .FirstOrDefaultAsync(v => v.TenantId == TenantConstants.DefaultTenantId, cancellationToken)
            .ConfigureAwait(false);

        if (vatSettings is not null)
        {
            return vatSettings;
        }

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
        return vatSettings;
    }

    internal static List<InvoiceLineItem> BuildLineItems(
        IEnumerable<BookingService> bookingServices,
        Dictionary<Guid, Service> services,
        decimal defaultVatRate)
    {
        return bookingServices
            .OrderBy(bs => bs.SortOrder)
            .Select(bs =>
            {
                var effectiveVatPercentage = defaultVatRate;
                if (services.TryGetValue(bs.ServiceId, out var service) && service.VatRate.HasValue)
                {
                    effectiveVatPercentage = service.VatRate.Value;
                }

                var unitPrice = bs.Price;
                var vatAmount = Math.Round(unitPrice * effectiveVatPercentage / 100m, 2, MidpointRounding.AwayFromZero);
                return new InvoiceLineItem
                {
                    Id = Guid.NewGuid(),
                    Description = bs.ServiceName,
                    Quantity = 1,
                    UnitPrice = unitPrice,
                    TotalPrice = unitPrice,
                    VatPercentage = effectiveVatPercentage,
                    VatAmount = vatAmount,
                    SortOrder = bs.SortOrder,
                    IsManual = false,
                };
            })
            .ToList();
    }
}
#pragma warning restore CA1812
