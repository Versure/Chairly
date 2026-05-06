namespace Chairly.Api.Features.Clients.GetClientTimeline;

internal sealed record ClientTimelineInvoiceResponse(
    Guid Id,
    string InvoiceNumber,
    DateOnly InvoiceDate,
    decimal TotalAmount,
    string Status,
    DateTimeOffset? SentAtUtc,
    DateTimeOffset? PaidAtUtc,
    DateTimeOffset? VoidedAtUtc);
