using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Tenancy;
using Chairly.Domain.Entities;
using Chairly.Domain.Enums;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

#pragma warning disable CA1812 // Instantiated via DI
namespace Chairly.Api.Features.Notifications.GetNotificationsList;

internal sealed class GetNotificationsListHandler(ChairlyDbContext db) : IRequestHandler<GetNotificationsListQuery, IReadOnlyList<NotificationResponse>>
{
    public async Task<IReadOnlyList<NotificationResponse>> Handle(GetNotificationsListQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var notifications = await db.Notifications
            .Where(n => n.TenantId == TenantConstants.DefaultTenantId)
            .OrderByDescending(n => n.CreatedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var clientIds = notifications
            .Where(n => n.RecipientType == RecipientType.Client)
            .Select(n => n.RecipientId)
            .Distinct()
            .ToList();

        var clients = await db.Clients
            .Where(c => clientIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, cancellationToken)
            .ConfigureAwait(false);

        return notifications.Select(n => ToResponse(n, clients)).ToList();
    }

    private static NotificationResponse ToResponse(Notification notification, Dictionary<Guid, Client> clients)
    {
        var recipientName = notification.RecipientType == RecipientType.Client
            && clients.TryGetValue(notification.RecipientId, out var client)
                ? $"{client.FirstName} {client.LastName}"
                : "Onbekend";

        return new NotificationResponse(
            notification.Id,
            notification.Type.ToString(),
            recipientName,
            notification.Channel.ToString(),
            DeriveStatus(notification),
            notification.ScheduledAtUtc,
            notification.SentAtUtc,
            notification.FailedAtUtc,
            notification.FailureReason,
            notification.RetryCount,
            notification.ReferenceId);
    }

    private static string DeriveStatus(Notification notification)
    {
        if (notification.SentAtUtc.HasValue)
        {
            return "Verzonden";
        }

        if (notification.FailedAtUtc.HasValue)
        {
            return "Mislukt";
        }

        return "Wachtend";
    }
}
#pragma warning restore CA1812
