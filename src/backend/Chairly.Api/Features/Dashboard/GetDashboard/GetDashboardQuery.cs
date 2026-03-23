using Chairly.Api.Shared.Mediator;

namespace Chairly.Api.Features.Dashboard.GetDashboard;

internal sealed record GetDashboardQuery : IRequest<DashboardResponse>;
