using Chairly.Api.Shared.Mediator;

namespace Chairly.Api.Features.Admin.UpdateSubscriptionPlan;

internal static class UpdateSubscriptionPlanEndpoint
{
    public static void MapUpdateSubscriptionPlan(this RouteGroupBuilder group)
    {
        group.MapPut("/{id:guid}/plan", async (
            Guid id,
            UpdateSubscriptionPlanCommand command,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            command.Id = id;
            var result = await mediator.Send(command, cancellationToken).ConfigureAwait(false);
            return result.Match(
                response => Results.Ok(response),
                _ => Results.NotFound(),
                unprocessable => Results.UnprocessableEntity(new { message = unprocessable.Message }));
        });
    }
}
