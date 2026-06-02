namespace Chairly.Api.Features.Newsletters;

internal sealed record NewsletterCampaignResponse(
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
    Guid CreatedBy,
    DateTimeOffset? UpdatedAtUtc,
    Guid? UpdatedBy);
