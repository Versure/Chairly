using Chairly.Api.Shared.Mediator;

namespace Chairly.Api.Features.Newsletters.ScheduleNewsletterCampaign;

internal static class ScheduleNewsletterCampaignEndpoint
{
    public static void MapScheduleNewsletterCampaign(this RouteGroupBuilder group)
    {
        group.MapPost("/campaigns/{id:guid}/schedule", async (
            Guid id,
            ScheduleNewsletterCampaignCommand command,
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
