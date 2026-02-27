using Chairly.Api.Shared.Mediator;
using OneOf;
using OneOf.Types;

namespace Chairly.Api.Features.Services.GetService;

internal sealed class GetServiceQuery(Guid id) : IRequest<OneOf<ServiceResponse, NotFound>>
{
    public Guid Id { get; } = id;
}
