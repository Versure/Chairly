using System.ComponentModel.DataAnnotations;
using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Results;
using OneOf;

#pragma warning disable CA1812 // Instantiated via ASP.NET Core model binding
namespace Chairly.Api.Features.Settings.UpdateCompanyInfo;

internal sealed class UpdateCompanyInfoCommand : IRequest<OneOf<CompanyInfoResponse, Forbidden>>
{
    [MaxLength(200)]
    public string? CompanyName { get; set; }

    [MaxLength(200)]
    [EmailAddress]
    public string? CompanyEmail { get; set; }

    [MaxLength(500)]
    public string? CompanyAddress { get; set; }

    [MaxLength(50)]
    public string? CompanyPhone { get; set; }

    [MaxLength(34)]
    public string? IbanNumber { get; set; }

    [MaxLength(50)]
    public string? VatNumber { get; set; }

    [Range(1, 365)]
    public int? PaymentPeriodDays { get; set; }
}
#pragma warning restore CA1812
