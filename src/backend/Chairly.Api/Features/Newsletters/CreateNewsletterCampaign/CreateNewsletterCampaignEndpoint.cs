using Chairly.Api.Shared.Mediator;

namespace Chairly.Api.Features.Newsletters.CreateNewsletterCampaign;

internal static class CreateNewsletterCampaignEndpoint
{
    public static void MapCreateNewsletterCampaign(this RouteGroupBuilder group)
    {
        group.MapPost("/campaigns", async (
            CreateNewsletterCampaignCommand command,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(command, cancellationToken).ConfigureAwait(false);
            return result.Match(
                response => Results.Created($"/api/newsletters/campaigns/{response.Id}", response),
                err => Results.UnprocessableEntity(new { message = err.Message }));
        });
    }
}
