using Chairly.Api.Features.Notifications.GetNotificationsList;

namespace Chairly.Api.Features.Notifications;

internal static class NotificationEndpoints
{
    public static IEndpointRouteBuilder MapNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/notifications")
            .RequireAuthorization("RequireStaff");

        group.MapGetNotificationsList();

        return app;
    }
}
