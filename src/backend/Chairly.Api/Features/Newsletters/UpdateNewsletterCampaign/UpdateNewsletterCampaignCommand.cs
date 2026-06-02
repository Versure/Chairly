using System.ComponentModel.DataAnnotations;
using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Results;
using OneOf;
using OneOf.Types;

#pragma warning disable CA1812
namespace Chairly.Api.Features.Newsletters.UpdateNewsletterCampaign;

internal sealed class UpdateNewsletterCampaignCommand : IRequest<OneOf<NewsletterCampaignResponse, NotFound, Conflict, Unprocessable>>
{
    public Guid Id { get; set; }

    [Required]
    [MaxLength(500)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    public string BodyHtml { get; set; } = string.Empty;
}
#pragma warning restore CA1812
