using Chairly.Domain.Entities;
using Chairly.Domain.Enums;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Chairly.Api.Features.Notifications.Infrastructure;

internal sealed partial class NotificationDispatcher(
    IServiceScopeFactory scopeFactory,
    ILogger<NotificationDispatcher> logger) : BackgroundService
{
    private const int PollIntervalSeconds = 60;
    private const int MaxRetryCount = 3;
    private const int BatchSize = 50;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DispatchPendingNotificationsAsync(stoppingToken).ConfigureAwait(false);
            }
#pragma warning disable CA1031 // Catch all exceptions to keep the background service running
            catch (Exception ex) when (ex is not OperationCanceledException)
#pragma warning restore CA1031
            {
                LogDispatchCycleFailed(logger, ex);
            }

            await Task.Delay(TimeSpan.FromSeconds(PollIntervalSeconds), stoppingToken).ConfigureAwait(false);
        }
    }

    internal async Task DispatchPendingNotificationsAsync(CancellationToken cancellationToken)
    {
        var scope = scopeFactory.CreateAsyncScope();
        try
        {
            var db = scope.ServiceProvider.GetRequiredService<ChairlyDbContext>();
            var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();

            var pendingNotifications = await FetchPendingNotificationsAsync(db, cancellationToken).ConfigureAwait(false);

            var sentCount = 0;
            var failedCount = 0;

            foreach (var notification in pendingNotifications)
            {
                var result = await TryDispatchNotificationAsync(db, emailSender, notification, cancellationToken).ConfigureAwait(false);
                if (result == DispatchResult.Sent)
                {
                    sentCount++;
                }
                else if (result == DispatchResult.Failed)
                {
                    failedCount++;
                }

                await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }

            if (pendingNotifications.Count > 0)
            {
                LogDispatchCycleSummary(logger, sentCount, failedCount, pendingNotifications.Count);
            }
        }
        finally
        {
            await scope.DisposeAsync().ConfigureAwait(false);
        }
    }

    private static async Task<List<Notification>> FetchPendingNotificationsAsync(ChairlyDbContext db, CancellationToken cancellationToken)
    {
        return await db.Notifications
            .Where(n => n.SentAtUtc == null
                && n.FailedAtUtc == null
                && n.ScheduledAtUtc <= DateTimeOffset.UtcNow
                && n.RetryCount < MaxRetryCount)
            .OrderBy(n => n.ScheduledAtUtc)
            .Take(BatchSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<DispatchResult> TryDispatchNotificationAsync(
        ChairlyDbContext db, IEmailSender emailSender, Notification notification, CancellationToken cancellationToken)
    {
        try
        {
            return await DispatchSingleNotificationAsync(db, emailSender, notification, cancellationToken).ConfigureAwait(false);
        }
#pragma warning disable CA1031 // Catch all to handle retry logic per notification
        catch (Exception ex) when (ex is not OperationCanceledException)
#pragma warning restore CA1031
        {
            HandleSendFailure(notification, ex);
            return DispatchResult.Failed;
        }
    }

    private async Task<DispatchResult> DispatchSingleNotificationAsync(
        ChairlyDbContext db, IEmailSender emailSender, Notification notification, CancellationToken cancellationToken)
    {
        var client = notification.RecipientType == RecipientType.Client
            ? await db.Clients.FirstOrDefaultAsync(c => c.Id == notification.RecipientId, cancellationToken).ConfigureAwait(false)
            : null;

        if (client is null || string.IsNullOrEmpty(client.Email))
        {
            LogRecipientEmailMissing(logger, notification.Id, notification.RecipientId);
            return DispatchResult.Skipped;
        }

        var clientName = $"{client.FirstName} {client.LastName}";
        var (subject, htmlBody) = await RenderTemplateAsync(db, notification, clientName, cancellationToken).ConfigureAwait(false);

        if (string.IsNullOrEmpty(subject))
        {
            return DispatchResult.Skipped;
        }

        await emailSender.SendAsync(client.Email, clientName, subject, htmlBody, cancellationToken).ConfigureAwait(false);

        notification.SentAtUtc = DateTimeOffset.UtcNow;
        return DispatchResult.Sent;
    }

    private static async Task<(string Subject, string HtmlBody)> RenderTemplateAsync(
        ChairlyDbContext db, Notification notification, string clientName, CancellationToken cancellationToken)
    {
        var booking = await db.Bookings
            .Include(b => b.BookingServices)
            .FirstOrDefaultAsync(b => b.Id == notification.ReferenceId, cancellationToken)
            .ConfigureAwait(false);

        var serviceSummary = booking is not null
            ? string.Join(", ", booking.BookingServices.OrderBy(bs => bs.SortOrder).Select(bs => bs.ServiceName))
            : string.Empty;

        var startTime = booking?.StartTime ?? notification.ScheduledAtUtc;

        var settings = await db.TenantSettings
            .FirstOrDefaultAsync(s => s.TenantId == notification.TenantId, cancellationToken)
            .ConfigureAwait(false);
        var salonName = settings?.CompanyName ?? "Uw salon";

        return notification.Type switch
        {
            NotificationType.BookingConfirmation => EmailTemplates.BookingConfirmation(clientName, startTime, serviceSummary, salonName),
            NotificationType.BookingReminder => EmailTemplates.BookingReminder(clientName, startTime, serviceSummary, salonName),
            NotificationType.BookingCancellation => EmailTemplates.BookingCancellation(clientName, startTime, salonName),
            _ => (string.Empty, string.Empty),
        };
    }

    private void HandleSendFailure(Notification notification, Exception ex)
    {
        notification.RetryCount++;
        if (notification.RetryCount >= MaxRetryCount)
        {
            notification.FailedAtUtc = DateTimeOffset.UtcNow;
            notification.FailureReason = ex.Message.Length > 1000 ? ex.Message[..1000] : ex.Message;
        }

        LogNotificationSendFailed(logger, notification.Id, notification.RetryCount, ex);
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Dispatch cycle failed")]
    private static partial void LogDispatchCycleFailed(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Recipient email missing for notification {NotificationId}, recipient {RecipientId}")]
    private static partial void LogRecipientEmailMissing(ILogger logger, Guid notificationId, Guid recipientId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to send notification {NotificationId}, retry count: {RetryCount}")]
    private static partial void LogNotificationSendFailed(ILogger logger, Guid notificationId, int retryCount, Exception exception);

    [LoggerMessage(Level = LogLevel.Information, Message = "Dispatch cycle complete: {SentCount} sent, {FailedCount} failed, {TotalCount} processed")]
    private static partial void LogDispatchCycleSummary(ILogger logger, int sentCount, int failedCount, int totalCount);

    private enum DispatchResult
    {
        Sent,
        Failed,
        Skipped,
    }
}
