using Chairly.Api.Shared.Mediator;

namespace Chairly.Api.Features.Admin.CancelSubscription;

internal static class CancelSubscriptionEndpoint
{
    public static void MapCancelAdminSubscription(this RouteGroupBuilder group)
    {
        group.MapPost("/{id:guid}/cancel", async (
            Guid id,
            CancelSubscriptionCommand command,
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
