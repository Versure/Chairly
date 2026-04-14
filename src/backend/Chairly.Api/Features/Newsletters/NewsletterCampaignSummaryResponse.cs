namespace Chairly.Api.Features.Newsletters;

internal sealed record NewsletterCampaignSummaryResponse(
    Guid Id,
    string Subject,
    string Status,
    int RecipientCount,
    int SentCount,
    int FailedCount,
    DateTimeOffset? ScheduledAtUtc,
    DateTimeOffset? SentAtUtc,
    DateTimeOffset CreatedAtUtc);
