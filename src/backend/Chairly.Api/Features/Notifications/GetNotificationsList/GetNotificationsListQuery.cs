using Chairly.Api.Shared.Mediator;

namespace Chairly.Api.Features.Notifications.GetNotificationsList;

internal sealed record GetNotificationsListQuery : IRequest<IReadOnlyList<NotificationResponse>>;
