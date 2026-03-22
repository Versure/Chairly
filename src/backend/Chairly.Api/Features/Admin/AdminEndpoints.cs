using Chairly.Api.Features.Admin.CancelSubscription;
using Chairly.Api.Features.Admin.GetAdminSubscription;
using Chairly.Api.Features.Admin.GetAdminSubscriptionsList;
using Chairly.Api.Features.Admin.ProvisionSubscription;
using Chairly.Api.Features.Admin.UpdateSubscriptionPlan;

namespace Chairly.Api.Features.Admin;

internal static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/subscriptions")
            .RequireAuthorization("RequirePlatformAdmin");

        group.MapGetAdminSubscriptionsList();
        group.MapGetAdminSubscription();
        group.MapProvisionSubscription();
        group.MapCancelAdminSubscription();
        group.MapUpdateSubscriptionPlan();

        return app;
    }
}
