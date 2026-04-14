using Chairly.Api.Shared.Mediator;

namespace Chairly.Api.Features.Newsletters.CancelNewsletterCampaign;

internal static class CancelNewsletterCampaignEndpoint
{
    public static void MapCancelNewsletterCampaign(this RouteGroupBuilder group)
    {
        group.MapPost("/campaigns/{id:guid}/cancel", async (
            Guid id,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new CancelNewsletterCampaignCommand(id), cancellationToken).ConfigureAwait(false);
            return result.Match(
                response => Results.Ok(response),
                _ => Results.NotFound(),
                conflict => Results.Conflict(new { message = conflict.Message }));
        });
    }
}
