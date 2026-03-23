using Chairly.Api.Shared.Mediator;

namespace Chairly.Api.Features.Config.GetAdminConfig;

internal sealed record GetAdminConfigQuery : IRequest<AdminConfigResponse>;
