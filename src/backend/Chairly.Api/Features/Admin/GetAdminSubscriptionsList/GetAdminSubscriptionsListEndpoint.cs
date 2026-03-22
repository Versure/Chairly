using Chairly.Api.Shared.Mediator;

namespace Chairly.Api.Features.Admin.GetAdminSubscriptionsList;

internal static class GetAdminSubscriptionsListEndpoint
{
    public static void MapGetAdminSubscriptionsList(this RouteGroupBuilder group)
    {
        group.MapGet("/", async (
            string? search,
            string? status,
            string? plan,
            int? page,
            int? pageSize,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var query = new GetAdminSubscriptionsListQuery(
                search,
                status,
                plan,
                page ?? 1,
                pageSize ?? 25);

            var result = await mediator.Send(query, cancellationToken).ConfigureAwait(false);
            return Results.Ok(result);
        });
    }
}
