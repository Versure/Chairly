using Chairly.Api.Shared.Mediator;

namespace Chairly.Api.Features.Newsletters.GetNewsletterCampaignDetail;

internal static class GetNewsletterCampaignDetailEndpoint
{
    public static void MapGetNewsletterCampaignDetail(this RouteGroupBuilder group)
    {
        group.MapGet("/campaigns/{id:guid}", async (
            Guid id,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new GetNewsletterCampaignDetailQuery(id), cancellationToken).ConfigureAwait(false);
            return result.Match(
                response => Results.Ok(response),
                _ => Results.NotFound());
        });
    }
}
