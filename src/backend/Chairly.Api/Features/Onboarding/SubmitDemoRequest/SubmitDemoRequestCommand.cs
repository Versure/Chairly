using System.ComponentModel.DataAnnotations;
using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Results;
using OneOf;

#pragma warning disable CA1812 // Instantiated via ASP.NET Core model binding
namespace Chairly.Api.Features.Onboarding.SubmitDemoRequest;

internal sealed class SubmitDemoRequestCommand : IRequest<OneOf<SubmitDemoRequestResponse, Unprocessable>>
{
    [Required]
    [MaxLength(200)]
    public string ContactName { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string SalonName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? PhoneNumber { get; set; }

    [MaxLength(2000)]
    public string? Message { get; set; }
}
#pragma warning restore CA1812
