using Chairly.Api.Features.Services.CreateServiceCategory;
using Chairly.Api.Features.Services.DeleteServiceCategory;
using Chairly.Api.Features.Services.GetServiceCategoriesList;
using Chairly.Api.Features.Services.UpdateServiceCategory;

namespace Chairly.Api.Features.Services;

internal static class ServiceCategoryEndpoints
{
    public static IEndpointRouteBuilder MapServiceCategoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/service-categories");

        group.MapCreateServiceCategory();
        group.MapGetServiceCategoriesList();
        group.MapUpdateServiceCategory();
        group.MapDeleteServiceCategory();

        return app;
    }
}
