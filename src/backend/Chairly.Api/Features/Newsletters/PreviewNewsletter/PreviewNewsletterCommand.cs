using System.ComponentModel.DataAnnotations;
using Chairly.Api.Shared.Mediator;

#pragma warning disable CA1812
namespace Chairly.Api.Features.Newsletters.PreviewNewsletter;

internal sealed class PreviewNewsletterCommand : IRequest<PreviewNewsletterResponse>
{
    [Required]
    [MaxLength(500)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    public string BodyHtml { get; set; } = string.Empty;
}
#pragma warning restore CA1812
