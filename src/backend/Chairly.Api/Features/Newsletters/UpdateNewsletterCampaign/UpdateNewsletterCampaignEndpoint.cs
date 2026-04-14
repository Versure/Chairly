using Chairly.Api.Shared.Mediator;

namespace Chairly.Api.Features.Newsletters.UpdateNewsletterCampaign;

internal static class UpdateNewsletterCampaignEndpoint
{
    public static void MapUpdateNewsletterCampaign(this RouteGroupBuilder group)
    {
        group.MapPut("/campaigns/{id:guid}", async (
            Guid id,
            UpdateNewsletterCampaignCommand command,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            command.Id = id;
            var result = await mediator.Send(command, cancellationToken).ConfigureAwait(false);
            return result.Match(
                response => Results.Ok(response),
                _ => Results.NotFound(),
                conflict => Results.Conflict(new { message = conflict.Message }),
                err => Results.UnprocessableEntity(new { message = err.Message }));
        });
    }
}
