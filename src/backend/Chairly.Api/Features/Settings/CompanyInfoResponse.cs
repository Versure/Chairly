namespace Chairly.Api.Features.Settings;

internal sealed record CompanyInfoResponse(
    string? CompanyName,
    string? CompanyEmail,
    string? CompanyAddress,
    string? CompanyPhone,
    string? IbanNumber,
    string? VatNumber,
    int? PaymentPeriodDays);
