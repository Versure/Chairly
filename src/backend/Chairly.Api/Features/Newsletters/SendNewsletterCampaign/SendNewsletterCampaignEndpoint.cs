using Chairly.Api.Shared.Mediator;

namespace Chairly.Api.Features.Newsletters.SendNewsletterCampaign;

internal static class SendNewsletterCampaignEndpoint
{
    public static void MapSendNewsletterCampaign(this RouteGroupBuilder group)
    {
        group.MapPost("/campaigns/{id:guid}/send", async (
            Guid id,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new SendNewsletterCampaignCommand(id), cancellationToken).ConfigureAwait(false);
            return result.Match(
                response => Results.Accepted($"/api/newsletters/campaigns/{response.Id}", response),
                _ => Results.NotFound(),
                conflict => Results.Conflict(new { message = conflict.Message }),
                err => Results.UnprocessableEntity(new { message = err.Message }));
        });
    }
}
