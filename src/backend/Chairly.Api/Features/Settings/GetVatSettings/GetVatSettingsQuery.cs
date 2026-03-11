using Chairly.Api.Shared.Mediator;

namespace Chairly.Api.Features.Settings.GetVatSettings;

internal sealed record GetVatSettingsQuery : IRequest<VatSettingsResponse>;
