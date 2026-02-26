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
        var group = app.MapGroup("/api/services");

        group.MapCreateService();
        group.MapGetServicesList();
        group.MapGetService();
        group.MapUpdateService();
        group.MapDeleteService();
        group.MapToggleServiceActive();

        return app;
    }
}
