using System.ComponentModel.DataAnnotations;
using Chairly.Api.Shared.Mediator;

#pragma warning disable CA1812 // Instantiated via ASP.NET Core model binding
namespace Chairly.Api.Features.Clients.CreateClient;

internal sealed class CreateClientCommand : IRequest<ClientResponse>
{
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [EmailAddress]
    [MaxLength(256)]
    public string? Email { get; set; }

    [MaxLength(50)]
    public string? PhoneNumber { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }
}
#pragma warning restore CA1812
