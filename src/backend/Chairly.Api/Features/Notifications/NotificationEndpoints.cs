using Chairly.Api.Features.Notifications.GetNotificationsList;

namespace Chairly.Api.Features.Notifications;

internal static class NotificationEndpoints
{
    public static IEndpointRouteBuilder MapNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/notifications");

        group.MapGetNotificationsList();

        return app;
    }
}
