namespace Chairly.Api.Features.Billing;

internal sealed record InvoiceResponse(
    Guid Id,
    string InvoiceNumber,
    DateOnly InvoiceDate,
    Guid BookingId,
    Guid ClientId,
    string ClientFullName,
    ClientSnapshotResponse ClientSnapshot,
    string StaffMemberName,
    decimal SubTotalAmount,
    decimal TotalVatAmount,
    decimal TotalAmount,
    string Status,
    string PaymentMethod,
    IReadOnlyList<InvoiceLineItemResponse> LineItems,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? SentAtUtc,
    DateTimeOffset? PaidAtUtc,
    DateTimeOffset? VoidedAtUtc);
