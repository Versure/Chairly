namespace Chairly.Api.Features.Settings;

internal sealed record CompanyInfoResponse(
    string? CompanyName,
    string? CompanyEmail,
    string? Street,
    string? HouseNumber,
    string? PostalCode,
    string? City,
    string? CompanyPhone,
    string? IbanNumber,
    string? VatNumber,
    int? PaymentPeriodDays);
