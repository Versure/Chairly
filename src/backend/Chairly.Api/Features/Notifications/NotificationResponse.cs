namespace Chairly.Api.Features.Notifications;

internal sealed record NotificationResponse(
    Guid Id,
    string Type,
    string RecipientName,
    string Channel,
    string Status,
    DateTimeOffset ScheduledAtUtc,
    DateTimeOffset? SentAtUtc,
    DateTimeOffset? FailedAtUtc,
    string? FailureReason,
    int RetryCount,
    Guid ReferenceId);
