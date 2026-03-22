using Chairly.Api.Shared.Mediator;

namespace Chairly.Api.Features.Admin.GetAdminSubscription;

internal static class GetAdminSubscriptionEndpoint
{
    public static void MapGetAdminSubscription(this RouteGroupBuilder group)
    {
        group.MapGet("/{id:guid}", async (
            Guid id,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new GetAdminSubscriptionQuery(id), cancellationToken).ConfigureAwait(false);
            return result.Match(
                response => Results.Ok(response),
                _ => Results.NotFound());
        });
    }
}
