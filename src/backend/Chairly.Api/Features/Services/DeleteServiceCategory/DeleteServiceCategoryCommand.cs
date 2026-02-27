using Chairly.Api.Shared.Mediator;
using OneOf;
using OneOf.Types;

namespace Chairly.Api.Features.Services.DeleteServiceCategory;

internal sealed class DeleteServiceCategoryCommand : IRequest<OneOf<Success, NotFound>>
{
    public Guid Id { get; set; }
}
