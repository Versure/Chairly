using Chairly.Api.Shared.Mediator;

#pragma warning disable CA1812 // Instantiated via ASP.NET Core model binding
namespace Chairly.Api.Features.Settings.UpdateVatSettings;

internal sealed class UpdateVatSettingsCommand : IRequest<VatSettingsResponse>
{
    public decimal DefaultVatRate { get; set; }
}
#pragma warning restore CA1812
