using Chairly.Api.Dispatching;

namespace Chairly.Api.Features.Services.GetServicesList;

internal sealed class GetServicesListQuery : IRequest<IEnumerable<ServiceResponse>>
{
}
