using System.ComponentModel.DataAnnotations;
using Chairly.Api.Shared.Mediator;

#pragma warning disable CA1812 // Instantiated via ASP.NET Core model binding
namespace Chairly.Api.Features.Services.CreateService;

internal sealed class CreateServiceCommand : IRequest<ServiceResponse>
{
    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    [Required]
    public TimeSpan Duration { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Price { get; set; }

    public decimal? VatRate { get; set; }

    public Guid? CategoryId { get; set; }

    public int SortOrder { get; set; }
}
#pragma warning restore CA1812
