using Chairly.Api.Shared.Mediator;

namespace Chairly.Api.Features.Notifications.GetNotificationsList;

internal static class GetNotificationsListEndpoint
{
    public static void MapGetNotificationsList(this RouteGroupBuilder group)
    {
        group.MapGet("/", async (
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new GetNotificationsListQuery(), cancellationToken).ConfigureAwait(false);
            return Results.Ok(result);
        });
    }
}
