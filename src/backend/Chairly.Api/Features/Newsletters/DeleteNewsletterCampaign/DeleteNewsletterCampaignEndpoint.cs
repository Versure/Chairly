using Chairly.Api.Shared.Mediator;

namespace Chairly.Api.Features.Newsletters.DeleteNewsletterCampaign;

internal static class DeleteNewsletterCampaignEndpoint
{
    public static void MapDeleteNewsletterCampaign(this RouteGroupBuilder group)
    {
        group.MapDelete("/campaigns/{id:guid}", async (
            Guid id,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new DeleteNewsletterCampaignCommand(id), cancellationToken).ConfigureAwait(false);
            return result.Match(
                _ => Results.NoContent(),
                _ => Results.NotFound(),
                conflict => Results.Conflict(new { message = conflict.Message }));
        });
    }
}
