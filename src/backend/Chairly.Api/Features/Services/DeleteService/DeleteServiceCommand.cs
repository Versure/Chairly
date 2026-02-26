using Chairly.Api.Dispatching;
using OneOf;
using OneOf.Types;

namespace Chairly.Api.Features.Services.DeleteService;

internal sealed class DeleteServiceCommand(Guid id) : IRequest<OneOf<Success, NotFound>>
{
    public Guid Id { get; } = id;
}
