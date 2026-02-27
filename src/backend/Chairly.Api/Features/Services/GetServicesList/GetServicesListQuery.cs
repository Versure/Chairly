using Chairly.Api.Shared.Mediator;

namespace Chairly.Api.Features.Services.GetServicesList;

internal sealed class GetServicesListQuery : IRequest<IEnumerable<ServiceResponse>>
{
}
