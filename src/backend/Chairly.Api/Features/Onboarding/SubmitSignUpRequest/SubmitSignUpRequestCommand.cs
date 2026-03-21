using System.ComponentModel.DataAnnotations;
using Chairly.Api.Shared.Mediator;

#pragma warning disable CA1812 // Instantiated via ASP.NET Core model binding
namespace Chairly.Api.Features.Onboarding.SubmitSignUpRequest;

internal sealed class SubmitSignUpRequestCommand : IRequest<SubmitSignUpRequestResponse>
{
    [Required]
    [MaxLength(200)]
    public string SalonName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string OwnerFirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string OwnerLastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? PhoneNumber { get; set; }
}
#pragma warning restore CA1812
