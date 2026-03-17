using System.ComponentModel.DataAnnotations;
using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Results;
using OneOf;

#pragma warning disable CA1812 // Instantiated via ASP.NET Core model binding
namespace Chairly.Api.Features.Tenants.ProvisionTenant;

internal sealed class ProvisionTenantCommand : IRequest<OneOf<ProvisionTenantResponse, Unprocessable>>
{
    [Required]
    public Guid TenantId { get; set; }

    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string OwnerEmail { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string OwnerFirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string OwnerLastName { get; set; } = string.Empty;
}
#pragma warning restore CA1812
