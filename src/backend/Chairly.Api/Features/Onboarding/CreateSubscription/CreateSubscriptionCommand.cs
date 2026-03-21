using System.ComponentModel.DataAnnotations;
using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Results;
using OneOf;

#pragma warning disable CA1812 // Instantiated via ASP.NET Core model binding
namespace Chairly.Api.Features.Onboarding.CreateSubscription;

internal sealed class CreateSubscriptionCommand : IRequest<OneOf<SubscriptionResponse, Unprocessable>>
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

    [Required]
    public string Plan { get; set; } = string.Empty;

    public string? BillingCycle { get; set; }

    public bool IsTrial { get; set; }
}
#pragma warning restore CA1812
