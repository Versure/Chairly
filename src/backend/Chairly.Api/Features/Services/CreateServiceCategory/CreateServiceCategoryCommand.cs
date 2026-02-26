using System.ComponentModel.DataAnnotations;
using Chairly.Api.Dispatching;

#pragma warning disable CA1812 // Instantiated via ASP.NET Core model binding
namespace Chairly.Api.Features.Services.CreateServiceCategory;

internal sealed class CreateServiceCategoryCommand : IRequest<ServiceCategoryResponse>
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public int SortOrder { get; set; }
}
