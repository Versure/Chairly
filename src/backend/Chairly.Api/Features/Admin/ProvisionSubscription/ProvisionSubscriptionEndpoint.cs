using Chairly.Api.Shared.Mediator;

namespace Chairly.Api.Features.Admin.ProvisionSubscription;

internal static class ProvisionSubscriptionEndpoint
{
    public static void MapProvisionSubscription(this RouteGroupBuilder group)
    {
        group.MapPost("/{id:guid}/provision", async (
            Guid id,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var command = new ProvisionSubscriptionCommand { Id = id };
            var result = await mediator.Send(command, cancellationToken).ConfigureAwait(false);
            return result.Match(
                response => Results.Ok(response),
                _ => Results.NotFound(),
                unprocessable => Results.UnprocessableEntity(new { message = unprocessable.Message }));
        });
    }
}
