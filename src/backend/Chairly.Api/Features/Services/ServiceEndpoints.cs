using Chairly.Api.Features.Services.CreateService;
using Chairly.Api.Features.Services.DeleteService;
using Chairly.Api.Features.Services.GetService;
using Chairly.Api.Features.Services.GetServicesList;
using Chairly.Api.Features.Services.ToggleServiceActive;
using Chairly.Api.Features.Services.UpdateService;

namespace Chairly.Api.Features.Services;

internal static class ServiceEndpoints
{
    public static IEndpointRouteBuilder MapServiceEndpoints(this IEndpointRouteBuilder app)
    {
        var readGroup = app.MapGroup("/api/services")
            .RequireAuthorization("RequireStaff");

        readGroup.MapGetServicesList();
        readGroup.MapGetService();

        var writeGroup = app.MapGroup("/api/services")
            .RequireAuthorization("RequireManager");

        writeGroup.MapCreateService();
        writeGroup.MapUpdateService();
        writeGroup.MapDeleteService();
        writeGroup.MapToggleServiceActive();

        return app;
    }
}
