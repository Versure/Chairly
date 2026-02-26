using System.ComponentModel.DataAnnotations;
using Chairly.Api.Dispatching;
using OneOf;
using OneOf.Types;

#pragma warning disable CA1812 // Instantiated via ASP.NET Core model binding
namespace Chairly.Api.Features.Services.UpdateServiceCategory;

internal sealed class UpdateServiceCategoryCommand : IRequest<OneOf<ServiceCategoryResponse, NotFound>>
{
    public Guid Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public int SortOrder { get; set; }
}
