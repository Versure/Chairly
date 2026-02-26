using Chairly.Api.Dispatching;
using OneOf;
using OneOf.Types;

namespace Chairly.Api.Features.Services.ToggleServiceActive;

internal sealed class ToggleServiceActiveCommand(Guid id) : IRequest<OneOf<ServiceResponse, NotFound>>
{
    public Guid Id { get; } = id;
}
