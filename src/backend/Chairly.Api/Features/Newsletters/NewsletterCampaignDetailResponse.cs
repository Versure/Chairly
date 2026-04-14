namespace Chairly.Api.Features.Newsletters;

internal sealed record NewsletterCampaignDetailResponse(
    Guid Id,
    string Subject,
    string BodyHtml,
    string RecipientFilter,
    string Status,
    DateTimeOffset? ScheduledAtUtc,
    DateTimeOffset? QueuedAtUtc,
    DateTimeOffset? SentAtUtc,
    DateTimeOffset? CancelledAtUtc,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc,
    int TotalRecipients,
    int SentCount,
    int FailedCount,
    int PendingCount,
    int UnsubscribedCount,
    int EligibleSubscribers);
