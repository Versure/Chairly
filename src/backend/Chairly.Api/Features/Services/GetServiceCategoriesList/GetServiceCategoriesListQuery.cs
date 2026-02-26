using Chairly.Api.Dispatching;

namespace Chairly.Api.Features.Services.GetServiceCategoriesList;

internal sealed class GetServiceCategoriesListQuery : IRequest<IEnumerable<ServiceCategoryResponse>>
{
}
