using System.ComponentModel.DataAnnotations;
using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Results;
using OneOf;

#pragma warning disable CA1812 // Instantiated via ASP.NET Core model binding
namespace Chairly.Api.Features.Newsletters.CreateNewsletterCampaign;

internal sealed class CreateNewsletterCampaignCommand : IRequest<OneOf<NewsletterCampaignResponse, Unprocessable>>
{
    [Required]
    [MaxLength(500)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    public string BodyHtml { get; set; } = string.Empty;
}
#pragma warning restore CA1812
