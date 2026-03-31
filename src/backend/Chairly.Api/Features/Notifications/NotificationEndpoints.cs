using Chairly.Api.Features.Notifications.GetEmailTemplatesList;
using Chairly.Api.Features.Notifications.GetNotificationsList;
using Chairly.Api.Features.Notifications.PreviewEmailTemplate;
using Chairly.Api.Features.Notifications.ResetEmailTemplate;
using Chairly.Api.Features.Notifications.UpdateEmailTemplate;

namespace Chairly.Api.Features.Notifications;

internal static class NotificationEndpoints
{
    public static IEndpointRouteBuilder MapNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/notifications")
            .RequireAuthorization("RequireStaff");

        group.MapGetNotificationsList();

        var emailTemplatesGroup = app.MapGroup("/api/notifications/email-templates")
            .RequireAuthorization("RequireManager");

        emailTemplatesGroup.MapGetEmailTemplatesList();
        emailTemplatesGroup.MapUpdateEmailTemplate();
        emailTemplatesGroup.MapResetEmailTemplate();
        emailTemplatesGroup.MapPreviewEmailTemplate();

        return app;
    }
}
