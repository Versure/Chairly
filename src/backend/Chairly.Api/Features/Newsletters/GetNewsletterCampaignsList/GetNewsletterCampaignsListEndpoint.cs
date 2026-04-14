using Chairly.Api.Shared.Mediator;

namespace Chairly.Api.Features.Newsletters.GetNewsletterCampaignsList;

internal static class GetNewsletterCampaignsListEndpoint
{
    public static void MapGetNewsletterCampaignsList(this RouteGroupBuilder group)
    {
        group.MapGet("/campaigns", async (
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new GetNewsletterCampaignsListQuery(), cancellationToken).ConfigureAwait(false);
            return Results.Ok(result);
        });
    }
}
