using Chairly.Api.Features.Services.CreateServiceCategory;
using Chairly.Api.Features.Services.DeleteServiceCategory;
using Chairly.Api.Features.Services.GetServiceCategoriesList;
using Chairly.Api.Features.Services.UpdateServiceCategory;

namespace Chairly.Api.Features.Services;

internal static class ServiceCategoryEndpoints
{
    public static IEndpointRouteBuilder MapServiceCategoryEndpoints(this IEndpointRouteBuilder app)
    {
        var readGroup = app.MapGroup("/api/service-categories")
            .RequireAuthorization("RequireStaff");

        readGroup.MapGetServiceCategoriesList();

        var writeGroup = app.MapGroup("/api/service-categories")
            .RequireAuthorization("RequireManager");

        writeGroup.MapCreateServiceCategory();
        writeGroup.MapUpdateServiceCategory();
        writeGroup.MapDeleteServiceCategory();

        return app;
    }
}
