using Chairly.Api.Shared.Mediator;

namespace Chairly.Api.Features.Services.GetServiceCategoriesList;

internal sealed class GetServiceCategoriesListQuery : IRequest<IEnumerable<ServiceCategoryResponse>>
{
}
