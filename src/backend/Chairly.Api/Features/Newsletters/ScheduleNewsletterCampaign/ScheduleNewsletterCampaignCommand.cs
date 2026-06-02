using System.ComponentModel.DataAnnotations;
using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Results;
using OneOf;
using OneOf.Types;

#pragma warning disable CA1812
namespace Chairly.Api.Features.Newsletters.ScheduleNewsletterCampaign;

internal sealed class ScheduleNewsletterCampaignCommand : IRequest<OneOf<NewsletterCampaignResponse, NotFound, Conflict, Unprocessable>>
{
    public Guid Id { get; set; }

    [Required]
    public DateTimeOffset ScheduledAtUtc { get; set; }
}
#pragma warning restore CA1812
