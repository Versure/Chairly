namespace Chairly.Api.Features.Billing;

internal sealed record InvoiceLineItemResponse(
    Guid Id,
    string Description,
    int Quantity,
    decimal UnitPrice,
    decimal TotalPrice,
    int SortOrder);
